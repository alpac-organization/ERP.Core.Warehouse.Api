using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class RegisterPurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, IS3StorageService _s3StorageService, ILogger<RegisterPurchaseRequestHandler> _logger) : BaseValidatorHandler<RegisterPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
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
            
            _logger.LogInformation("Iniciando proceso para registro de solicitud de compras");

            foreach (var purchaseRequest in request.PurchaseRequests)
            {
                Guid areaId = access.Profile.AreaId;

                if (access.Role?.RoleType == RoleType.Administrator && purchaseRequest.AreaId.HasValue)
                {
                    areaId = purchaseRequest.AreaId.Value;
                }

                var (isSucceded, code) = await _codeGenerator.GenerateUniqueCodeToPurchaseRequest(
                    purchaseRequest.RequestType, 
                    access.Profile.BranchId,
                    cancellationToken
                );

                if (!isSucceded)
                {
                    throw new InvalidOperationException("Ocurrio un error la generar la solucitud de comprar");
                }

                var itemProductIds = purchaseRequest.PurchaseRequestItems
                    .Select(item => item.ProductId)
                    .Distinct()
                    .ToList();

                #region Evaluación de los productos

                var productsById = await _unitOfWork.Products.Entities
                    .AsNoTracking()
                    .Where(product =>
                        itemProductIds.Contains(product.Id) &&
                        product.ProductStatus == ProductStatus.Active &&
                        product.DeletedAt == null)
                    .Select(product => new
                    {
                        product.Id,
                        product.ProductUsageType,
                        product.UnitMeasureId
                    })
                    .ToDictionaryAsync(product => product.Id, cancellationToken);

                if (productsById.Count != itemProductIds.Count)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "Uno o más productos seleccionados no existen o no están activos.",
                        "ERP:PRODUCT_NOT_FOUND");
                }

                if (productsById.Values.Any(product => product.UnitMeasureId == Guid.Empty))
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "Uno o más productos no tienen unidad de medida asociada.",
                        "ERP:UNIT_MEASURE_REQUIRED");
                }

                var expectedUsageType = purchaseRequest.Destination == DestinationRequest.ServiceOrder
                    ? ProductUsageType.Operational
                    : ProductUsageType.Administrative;

                if (productsById.Values.Any(product => product.ProductUsageType != expectedUsageType))
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "Uno o más productos no corresponden al destino de la solicitud.",
                        "ERP:INVALID_PRODUCT_USAGE_TYPE");
                }

                #endregion

                var purchaseRequestEntity = PurchaseRequestMapper.ToPurchaseRequestEntity(purchaseRequest, code, access.Profile);
                await _unitOfWork.PurchaseRequests.RegisterPurchaseRequest(purchaseRequestEntity);

                foreach (var productItem in purchaseRequest.PurchaseRequestItems)
                {
                    await UploadItemImagesAsync(productItem, cancellationToken);

                    var productUnitMeasureId = productsById[productItem.ProductId].UnitMeasureId;

                    var purchaseRequestItemEntity = PurchaseRequestMapper.ToPurchaseRequestItemEntity(
                        productItem,
                        purchaseRequestEntity.Id,
                        productUnitMeasureId
                    );

                    await _unitOfWork.PurchaseRequestItems.RegisterPurchaseRequestItem(purchaseRequestItemEntity);
                }
            }
            
            _logger.LogInformation("Se registro exitosamente la solicitud de compra");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task UploadItemImagesAsync(Commands.PurchaseRequestItem productItem, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(productItem.AdditionalData))
            {
                return;
            }

            var additionalData = JsonSerializer.Deserialize<PurchaseRequestItemAdditionalData>(
                productItem.AdditionalData,
                JsonManager.SnakeCaseOptions
            );

            if (additionalData is null || additionalData.ImagesProductToChanged.Count == 0)
            {
                return;
            }

            var uploadedUrls = new List<string>(additionalData.ImagesProductToChanged.Count);

            foreach (var image in additionalData.ImagesProductToChanged)
            {
                if (string.IsNullOrWhiteSpace(image))
                {
                    continue;
                }

                if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    uploadedUrls.Add(image);
                    continue;
                }

                var imageUrl = await _s3StorageService.UploadImageAsync(
                    "Compras",
                    "SolicitudesCompras",
                    image,
                    cancellationToken
                );

                uploadedUrls.Add(imageUrl);
            }

            additionalData.ImagesProductToChanged = uploadedUrls;
            productItem.AdditionalData = JsonSerializer.Serialize(additionalData, JsonManager.SnakeCaseOptions);
        }
    }
}
