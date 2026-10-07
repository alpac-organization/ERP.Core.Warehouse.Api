using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Services;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class UpdateQuotationHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        ILogger<UpdateQuotationHandler> _logger,
        IS3StorageService _s3StorageService
    ) : BaseValidatorHandler<UpdateQuotationCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<bool> Handle(UpdateQuotationCommand request, CancellationToken cancellationToken)
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

            _logger.LogInformation("Iniciando actualización de cotización.");

            var quotation = await _unitOfWork.Quotations.Entities
                .Include(quo => quo.PurchaseRequestItem)
                    .ThenInclude(item => item.Product)
                .Include(quo => quo.SupplierProduct)
                    .ThenInclude(sp => sp!.TierPrices)
                .Where(quo => quo.IsActive)
                .Where(quo => quo.Id == request.QuotationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotation is null)
            {
                return _errorManager.ThrowNotFound<bool>("La cotización no existe.", "ERP:QUOTATION_NOT_FOUND");
            }

            if (request.SupplierId.HasValue)
                quotation.SupplierId = request.SupplierId.Value;

            if (request.HasDelivery.HasValue)
                quotation.HasDelivery = request.HasDelivery.Value;

            if (request.HasGuarantee.HasValue)
                quotation.HasGuarantee = request.HasGuarantee.Value;

            if (request.InventoryAvailable.HasValue)
                quotation.InventoryAvailable = request.InventoryAvailable.Value;

            if (request.BrandProduct is not null)
                quotation.BrandProduct = request.BrandProduct;

            if (request.ProductQuality.HasValue)
                quotation.ProductQuality = request.ProductQuality.Value;

            if (request.PaymentMethodType.HasValue)
                quotation.PaymentMethodType = request.PaymentMethodType.Value;

            if (request.DeliveryTime.HasValue)
                quotation.DeliveryTime = request.DeliveryTime.Value;

            if (request.DeliveryTimeType.HasValue)
                quotation.DeliveryTimeType = request.DeliveryTimeType.Value;

            if (request.WarrantyPeriod.HasValue)
                quotation.WarrantyPeriod = request.WarrantyPeriod.Value;

            if (request.WarrantyPeriodTimeType.HasValue)
                quotation.WarrantyPeriodTimeType = request.WarrantyPeriodTimeType.Value;

            if (request.AvailabilityTime.HasValue)
                quotation.AvailabilityTime = request.AvailabilityTime.Value;

            if (request.AvailabilityTimeType.HasValue)
                quotation.AvailabilityTimeType = request.AvailabilityTimeType.Value;

            if (request.SupplierSelectionJustification is not null)
                quotation.SupplierSelectionJustification = request.SupplierSelectionJustification;

            var quantity = quotation.PurchaseRequestItem?.Quantity ?? 0;
            var quoteDate = quotation.QuoteDate == default
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : quotation.QuoteDate;

            if (request.PriceUnit.HasValue)
            {
                quotation.PriceUnit = request.PriceUnit.Value;
            }
            else if (quotation.SupplierProduct is not null && quantity > 0)
            {
                quotation.PriceUnit = PurchaseTaxPricingService.ResolveUnitPrice(
                    quotation.SupplierProduct,
                    quantity,
                    quoteDate);
            }

            if (quantity > 0 && quotation.PriceUnit > 0)
            {
                var subtotal = Math.Round(quantity * quotation.PriceUnit, 2, MidpointRounding.AwayFromZero);
                quotation.PriceTotal = subtotal;

                var ivaRate = await PurchaseTaxPricingService.GetActiveTaxValueAsync(_unitOfWork, TaxType.Iva, cancellationToken);
                quotation.Iva = PurchaseTaxPricingService.CalculateIvaAmount(
                    subtotal,
                    quotation.PurchaseRequestItem?.Product?.IsTaxExempt ?? false,
                    ivaRate);
            }

            if (request.Images is { Count: > 0 } || request.Documents is { Count: > 0 })
            {
                quotation.AdditionalData = await MergeAdditionalDataAsync(
                    quotation.AdditionalData,
                    request.Images,
                    request.Documents,
                    cancellationToken);
            }

            await _unitOfWork.Quotations.UpdateAsync(quotation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cotización actualizada con éxito");
            return true;
        }

        private async Task<string> MergeAdditionalDataAsync(
            string? existingJson,
            List<QuotationFileInput>? images,
            List<QuotationFileInput>? documents,
            CancellationToken cancellationToken)
        {
            var additionalData = string.IsNullOrWhiteSpace(existingJson)
                ? new QuotationAdditionalData()
                : JsonSerializer.Deserialize<QuotationAdditionalData>(existingJson, JsonOptions) ?? new QuotationAdditionalData();

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
