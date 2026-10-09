using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Services;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class RegisterPurchaseRequestHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        ICodeGenerator _codeGenerator,
        IS3StorageService _s3StorageService,
        ILogger<RegisterPurchaseRequestHandler> _logger,
        ISimpleNotificationServices _simpleNotificationServices,
        IOptions<PurchaseRequestOptions> _options,
        IPurchasePeriodService _purchasePeriodService
    ) : BaseValidatorHandler<RegisterPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<bool> Handle(RegisterPurchaseRequestCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var existingProductIds = request.PurchaseRequests
                .SelectMany(pr => pr.PurchaseRequestItems)
                .Where(item => item.NewProduct is null && item.ProductId.HasValue)
                .Select(item => item.ProductId!.Value)
                .Distinct()
                .ToList();

            var productsById = existingProductIds.Count == 0
                ? new Dictionary<Guid, Product>()
                : await _unitOfWork.Products.Entities
                    .Include(p => p.SupplierProducts.Where(sp => sp.DeletedAt == null))
                    .Where(p => existingProductIds.Contains(p.Id) && p.DeletedAt == null)
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

            var additionalSupplierIds = request.PurchaseRequests
                .SelectMany(pr => pr.PurchaseRequestItems)
                .Where(item => item.AdditionalSupplierIds is { Count: > 0 })
                .SelectMany(item => item.AdditionalSupplierIds!)
                .Distinct()
                .ToList();

            HashSet<Guid>? validAdditionalSuppliers = null;
            if (additionalSupplierIds.Count > 0)
            {
                validAdditionalSuppliers = (await _unitOfWork.Suppliers.Entities
                    .Where(s => additionalSupplierIds.Contains(s.Id) && s.DeletedAt == null && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken))
                    .ToHashSet();

                if (validAdditionalSuppliers.Count != additionalSupplierIds.Count)
                {
                    return _errorManager.ThrowNotFound<bool>(
                        "Uno o más proveedores adicionales no existen o están inactivos",
                        "ERP:SUPPLIER_NOT_FOUND");
                }
            }

            foreach(var purchaseRequest in request.PurchaseRequests)
            {
                Guid areaId = access.Profile.AreaId;

                if (access.Role?.RoleType == RoleType.Administrator && purchaseRequest.AreaId.HasValue)
                {
                    areaId = purchaseRequest.AreaId.Value;
                }

                var (isSucceded, code) = await _codeGenerator.GenerateUniqueCodeToPurchaseRequest(purchaseRequest.RequestType, purchaseRequest.BranchId);

                if (!isSucceded)
                {
                    return _errorManager.ThrowBadRequest<bool>("Ocurrio un error la generar la solucitud de comprar", "ERP:ERROR_CODE_GENERATOR");
                }

                var requestDate = _purchasePeriodService.ResolveRequestPeriod(
                    purchaseRequest.RequestType,
                    DateOnly.FromDateTime(DateTime.UtcNow));

                var purchaseRequestEntity = PurchaseRequestMapper.ToPurchaseRequestEntity(
                    purchaseRequest,
                    code,
                    areaId,
                    access.User.Id,
                    requestDate);
                await _unitOfWork.PurchaseRequests.RegisterPurchaseRequest(purchaseRequestEntity);

                // Guardame imagenes en el S3 Bucket
                foreach (var productItem in purchaseRequest.PurchaseRequestItems)
                {
                    var (productId, unitMeasureId, resolveError) = await ResolveProductAsync(
                        productItem,
                        request.CompanyId,
                        productsById,
                        validAdditionalSuppliers,
                        cancellationToken);

                    if (resolveError is not null)
                    {
                        return resolveError.Value;
                    }

                    var purchaseRequestItemEntity = PurchaseRequestMapper.ToPurchaseRequestItemEntity(
                        productItem,
                        purchaseRequestEntity.Id,
                        productId,
                        unitMeasureId);

                    if (!string.IsNullOrWhiteSpace(purchaseRequestItemEntity.AdditionalData))
                    {
                        var additionalData = JsonSerializer.Deserialize<PurchaseRequestItemAdditionalData>(purchaseRequestItemEntity.AdditionalData, JsonOptions);

                        if (additionalData?.ImagesProductToChanged is { Count: > 0 })
                        {
                            var uploadedUrls = new List<string>();

                            foreach (var base64Image in additionalData.ImagesProductToChanged)
                            {
                                var imageUrl = await _s3StorageService.UploadImageAsync("Compras", "SolicitudesCompras", base64Image, cancellationToken);
                                uploadedUrls.Add(imageUrl);
                            }

                            additionalData.ImagesProductToChanged = uploadedUrls;
                            purchaseRequestItemEntity.AdditionalData = JsonSerializer.Serialize(additionalData, JsonOptions);
                        }
                    }

                    await _unitOfWork.PurchaseRequestItems.RegisterPurchaseRequestItem(purchaseRequestItemEntity);
                }
            }

            #region Enviar push notification

            var notificationConfig = _options.Value;

            string userName = access.User?.Fullname ?? "Un usuario";
            string descriptionCopy = (notificationConfig.Description ?? "{{Name}} registró una nueva solicitud de compra {{Type}}.")
                .Replace("{{Name}}", userName);

            var targetProfiles = await _unitOfWork.Profiles.Entities
                .Include(p => p.UserModuleRole)
                    .ThenInclude(umr => umr.Role)
                .Where(p => p.IsActive)
                .Where(p => p.UserId != request.UserId)
                .Where(p => p.CompanyId == request.CompanyId)
                .Where(p => p.UserModuleRole.Any(
                        umr => umr.ModuleCode == request.ModuleCode && (
                            umr.Role.RoleType == RoleType.Administrator ||
                            umr.Role.RoleType == RoleType.Manager
                        )
                    )
                )
                .ToListAsync(cancellationToken);

            var targetProfileIds = targetProfiles.Select(p => p.Id).ToList();

            foreach (var profile in targetProfiles)
            {
                await _unitOfWork.Notifications.CreateNotification(new()
                {
                    Title = notificationConfig.Title,
                    Description = descriptionCopy,
                    PathRedirect = "/purchasing",
                    AdditionalData = JsonSerializer.Serialize("{}"),
                    UserId = profile.UserId,
                });
            }

            var devices = await _unitOfWork.Devices.Entities
                .Where(device => device.IsActive)
                .Where(device => targetProfileIds.Contains(device.UserProfileId))
                .ToListAsync(cancellationToken);

            foreach (var device in devices)
            {
                var result = await _simpleNotificationServices.SendPushNotificationAsync(device?.EndpointArn ?? "", new()
                {
                    Title = notificationConfig?.Title ?? "",
                    Body = descriptionCopy ?? "",
                    WebPushConfig = new()
                    {
                        Badge = access?.Profile?.Company?.ImageUrl ?? "",
                        Icon = access?.Profile?.Company?.ImageUrl ?? ""
                    },
                    AndroidConfig = new()
                    {
                        Badge = access?.Profile?.Company?.ImageUrl ?? "",
                        Icon = access?.Profile?.Company?.ImageUrl ?? ""
                    }
                });

                _logger.LogInformation("Push notification result for device {EndpointArn}: {@Result}", device?.EndpointArn, result);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Se registro exitosamente la solicitud de compra");

            #endregion

            return true;
        }
        private async Task<(Guid ProductId, Guid UnitMeasureId, bool? ErrorResponse)> ResolveProductAsync(
            Commands.PurchaseRequestItem item,
            Guid companyId,
            Dictionary<Guid, Product> productsById,
            HashSet<Guid>? validAdditionalSuppliers,
            CancellationToken cancellationToken)
        {
            if (item.NewProduct is null)
            {
                var existingProductId = item.ProductId!.Value;

                if (!productsById.TryGetValue(existingProductId, out var product))
                {
                    return (Guid.Empty, Guid.Empty, _errorManager.ThrowNotFound<bool>(
                        "El producto seleccionado no existe", "ERP:PRODUCT_NOT_FOUND"));
                }

                if (item.AdditionalSupplierIds is { Count: > 0 })
                {
                    var linkTimestamp = DateTime.UtcNow;
                    var linked = false;

                    foreach (var supplierId in item.AdditionalSupplierIds.Distinct())
                    {
                        if (validAdditionalSuppliers is null || !validAdditionalSuppliers.Contains(supplierId))
                        {
                            return (Guid.Empty, Guid.Empty, _errorManager.ThrowNotFound<bool>(
                                "Uno o más proveedores adicionales no existen o están inactivos",
                                "ERP:SUPPLIER_NOT_FOUND"));
                        }

                        var alreadyLinked = product.SupplierProducts
                            .Any(sp => sp.SupplierId == supplierId && sp.DeletedAt == null);

                        if (alreadyLinked)
                        {
                            continue;
                        }

                        product.SupplierProducts.Add(new SupplierProduct
                        {
                            Id = Guid.NewGuid(),
                            IsActive = true,
                            ProductId = product.Id,
                            SupplierId = supplierId,
                            UnitPrice = 0m,
                            LastPriceUpdate = linkTimestamp
                        });
                        linked = true;
                    }

                    if (linked)
                    {
                        await _unitOfWork.Products.UpdateAsync(product);
                    }
                }

                var unitMeasureId = item.UnitMeasureId ?? product.UnitMeasureId;

                return (existingProductId, unitMeasureId, null);
            }

            var payload = item.NewProduct;

            var categoryExists = await _unitOfWork.CategoryProducts.Entities
                .AnyAsync(c => c.Id == payload.CategoryId && c.DeletedAt == null, cancellationToken);

            if (!categoryExists)
            {
                return (Guid.Empty, Guid.Empty, _errorManager.ThrowNotFound<bool>(
                    "La categoría del producto no existe", "ERP:CATEGORY_NOT_FOUND"));
            }

            var unitExists = await _unitOfWork.UnitsMeasurement.Entities
                .AnyAsync(u => u.Id == payload.UnitMeasureId && u.DeletedAt == null, cancellationToken);

            if (!unitExists)
            {
                return (Guid.Empty, Guid.Empty, _errorManager.ThrowNotFound<bool>(
                    "La unidad de medida del producto no existe", "ERP:UNIT_MEASURE_NOT_FOUND"));
            }

            if (payload.Suppliers.Count > 0)
            {
                var supplierIds = payload.Suppliers.Select(s => s.SupplierId).Distinct().ToList();
                var existingSupplierCount = await _unitOfWork.Suppliers.Entities
                    .CountAsync(s => supplierIds.Contains(s.Id) && s.DeletedAt == null, cancellationToken);

                if (existingSupplierCount != supplierIds.Count)
                {
                    return (Guid.Empty, Guid.Empty, _errorManager.ThrowNotFound<bool>(
                        "Uno o más proveedores seleccionados no existen", "ERP:SUPPLIER_NOT_FOUND"));
                }
            }

            var (codeSucceeded, productCode) = await _codeGenerator.GenerateUniqueProductCode(
                companyId,
                payload.CategoryId,
                cancellationToken);

            if (!codeSucceeded || string.IsNullOrWhiteSpace(productCode))
            {
                return (Guid.Empty, Guid.Empty, _errorManager.ThrowBadRequest<bool>(
                    "Ocurrió un error al generar el código del producto", "ERP:ERROR_PRODUCT_CODE_GENERATOR"));
            }

            var now = DateTime.UtcNow;
            var productId = Guid.NewGuid();

            var productEntity = new Product
            {
                Id = productId,
                Code = productCode,
                ProductName = payload.ProductName.Trim(),
                Description = payload.Description,
                CategoryId = payload.CategoryId,
                UnitMeasureId = payload.UnitMeasureId,
                ProductUsageType = payload.ProductUsageType,
                IsTaxExempt = payload.IsTaxExempt,
                SupplierProducts = payload.Suppliers
                    .GroupBy(s => s.SupplierId)
                    .Select(g =>
                    {
                        var link = g.First();
                        return new SupplierProduct
                        {
                            Id = Guid.NewGuid(),
                            IsActive = true,
                            ProductId = productId,
                            SupplierId = link.SupplierId,
                            UnitPrice = link.UnitPrice ?? 0m,
                            LastPriceUpdate = now
                        };
                    })
                    .ToList()
            };

            await _unitOfWork.Products.InsertProduct(productEntity);

            var itemUnitMeasureId = item.UnitMeasureId ?? payload.UnitMeasureId;

            return (productId, itemUnitMeasureId, null);
        }
    }
}