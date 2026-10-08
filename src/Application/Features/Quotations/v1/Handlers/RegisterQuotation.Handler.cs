using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using System.Text.Json;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Services;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class RegisterQuotationHandler(
        IUnitOfWork _unitOfWork, 
        IErrorManager _errorManager, 
        ILogger<RegisterQuotationHandler> _logger,
        IS3StorageService _s3StorageService) :  BaseValidatorHandler<RegisterQuotationCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<bool> Handle(RegisterQuotationCommand request, CancellationToken cancellationToken)
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

            _logger.LogInformation("🚩Iniciando registro de cotizaciones.");

            var validityDeductions = await _unitOfWork.ValidityDeductions.Entities
                .Where(v => v.DeletedAt == null)
                .ToListAsync(cancellationToken);

            // var ivaDeduction = validityDeductions.FirstOrDefault(v => v.Name.Contains("IVA", StringComparison.OrdinalIgnoreCase));
            // var irDeduction = validityDeductions.FirstOrDefault(v => v.Name.Contains("IR", StringComparison.OrdinalIgnoreCase));
            // var imiDeduction = validityDeductions.FirstOrDefault(v => v.Name.Contains("IMI", StringComparison.OrdinalIgnoreCase));
            var ivaDeduction = validityDeductions.FirstOrDefault(); // TODO: Ajustar según la entidad real
            var irDeduction = validityDeductions.FirstOrDefault();
            var imiDeduction = validityDeductions.FirstOrDefault();

            foreach (var quotation in request.QuotationItems)
            {
                var item = await _unitOfWork.PurchaseRequestItems.Entities
                    .Include(pur => pur.Product)
                    .Where(pur => pur.Id == quotation.PurchaseRequestItemId)
                    .Where(pur => pur.DeletedAt == null)
                    .FirstOrDefaultAsync(cancellationToken);

                if (item is null)
                {
                    _logger.LogInformation("❌No se encontro la información del item requisado");
                    continue;
                }

                var supplier = await _unitOfWork.Suppliers.Entities
                    .Include(s => s.SupplierProducts.Where(sp => sp.ProductId == item.ProductId && sp.IsActive && sp.DeletedAt == null))
                    .Include(s => s.SupplierPaymentMethods.Where(spm => spm.IsActive && spm.DeletedAt == null))
                    .Where(s => s.Id == quotation.SupplierId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (supplier is null)
                {
                    _logger.LogInformation("❌No se encontro la información del proveedor");
                    continue;
                }

                var supplierProduct = supplier.SupplierProducts.FirstOrDefault();
                if (supplierProduct is null)
                {
                    supplierProduct = new SupplierProduct
                    {
                        Id = Guid.NewGuid(),
                        IsActive = true,
                        ProductId = item.ProductId,
                        SupplierId = supplier.Id,
                        UnitPrice = quotation.Price,
                        LastPriceUpdate = DateTime.UtcNow
                    };
                    supplier.SupplierProducts.Add(supplierProduct);
                }

                var priceUnit = supplierProduct.UnitPrice;
                var totalToPay = item.Quantity * priceUnit;

                var quotationEntity = QuotationsMapper.ToQuotationsEntity(quotation);
                quotationEntity.PriceTotal = totalToPay;

                if (!item.HasQuotation)
                {
                    item.HasQuotation = true;
                    await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);
                }

                await _unitOfWork.Quotations.RegisterQuotation(quotationEntity);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cotización agregada con éxito");
            return true;
        }

        private async Task<(SupplierProduct? SupplierProduct, bool? ErrorResponse)> EnsureSupplierProductAsync(
            Guid productId,
            Guid supplierId,
            bool createIfMissing,
            decimal? seedUnitPrice,
            CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Products.Entities
                .Include(p => p.SupplierProducts)
                    .ThenInclude(sp => sp.TierPrices)
                .Where(p => p.Id == productId && p.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                return (null, _errorManager.ThrowNotFound<bool>("El producto del ítem no existe", "ERP:PRODUCT_NOT_FOUND"));
            }

            var existing = product.SupplierProducts
                .FirstOrDefault(sp => sp.SupplierId == supplierId && sp.DeletedAt == null && sp.IsActive);

            if (existing is not null)
            {
                return (existing, null);
            }

            if (!createIfMissing)
            {
                return (null, null);
            }

            var link = new SupplierProduct
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                ProductId = productId,
                SupplierId = supplierId,
                UnitPrice = seedUnitPrice ?? 0m,
                LastPriceUpdate = DateTime.UtcNow,
                TierPrices = []
            };

            product.SupplierProducts.Add(link);
            await _unitOfWork.Products.UpdateAsync(product);

            return (link, null);
        }

        private async Task<string?> BuildAdditionalDataAsync(
            List<QuotationFileInput>? images,
            List<QuotationFileInput>? documents,
            CancellationToken cancellationToken)
        {
            if ((images is null || images.Count == 0) && (documents is null || documents.Count == 0))
            {
                return null;
            }

            var additionalData = new QuotationAdditionalData();

            if (images is { Count: > 0 })
            {
                foreach (var image in images)
                {
                    var base64 = StripDataUrlPrefix(image.Base64Content);
                    var url = await _s3StorageService.UploadImageAsync("Compras", "Cotizaciones", base64, cancellationToken);

                    additionalData.Images.Add(new QuotationFileInformation
                    {
                        FileId = Guid.NewGuid(),
                        FileName = string.IsNullOrWhiteSpace(image.FileName) ? $"image-{Guid.NewGuid():N}.jpg" : image.FileName,
                        FileUrl = url,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            if (documents is { Count: > 0 })
            {
                foreach (var document in documents)
                {
                    var base64 = StripDataUrlPrefix(document.Base64Content);
                    var bytes = Convert.FromBase64String(base64);
                    await using var stream = new MemoryStream(bytes);
                    var fileName = string.IsNullOrWhiteSpace(document.FileName)
                        ? $"documento-{Guid.NewGuid():N}.pdf"
                        : document.FileName;

                    var url = await _s3StorageService.UploadPdfAsync("Compras", "Cotizaciones", stream, fileName);

                    additionalData.Documents.Add(new QuotationFileInformation
                    {
                        FileId = Guid.NewGuid(),
                        FileName = fileName,
                        FileUrl = url,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            return JsonSerializer.Serialize(additionalData, JsonOptions);
        }

        private static string StripDataUrlPrefix(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var commaIndex = content.IndexOf(',');
            if (content.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            {
                return content[(commaIndex + 1)..];
            }

            return content;
        }
    }
}
