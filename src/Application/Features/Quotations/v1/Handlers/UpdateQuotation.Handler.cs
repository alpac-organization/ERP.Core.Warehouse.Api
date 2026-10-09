using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Services;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class UpdateQuotationHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        ILogger<UpdateQuotationHandler> logger,
        IPurchaseTaxService purchaseTaxService,
        IQuotationAttachmentService quotationAttachmentService)
        : BaseValidatorHandler<UpdateQuotationCommand, bool>(unitOfWork, errorManager)
    {
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

            logger.LogInformation("Iniciando actualización de cotización.");

            var quotation = await _unitOfWork.Quotations.Entities
                .Include(quo => quo.PurchaseRequestItem)
                    .ThenInclude(item => item.Product)
                .Include(quo => quo.SupplierProduct)
                    .ThenInclude(sp => sp!.TierPrices)
                .Include(quo => quo.Supplier)
                    .ThenInclude(s => s.SupplierPaymentMethods.Where(spm => spm.IsActive && spm.DeletedAt == null))
                .AsSplitQuery()
                .Where(quo => quo.IsActive)
                .Where(quo => quo.Id == request.QuotationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotation is null)
            {
                return _errorManager.ThrowNotFound<bool>("La cotización no existe.", "ERP:QUOTATION_NOT_FOUND");
            }

            if (request.SupplierId.HasValue && request.SupplierId.Value != quotation.SupplierId)
            {
                var supplier = await _unitOfWork.Suppliers.Entities
                    .Include(s => s.SupplierPaymentMethods.Where(spm => spm.IsActive && spm.DeletedAt == null))
                    .Include(s => s.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                        .ThenInclude(sp => sp.TierPrices)
                    .AsSplitQuery()
                    .Where(s => s.Id == request.SupplierId.Value && s.DeletedAt == null)
                    .FirstOrDefaultAsync(cancellationToken);

                if (supplier is null)
                {
                    return _errorManager.ThrowNotFound<bool>("El proveedor no existe.", "ERP:SUPPLIER_NOT_FOUND");
                }

                var productId = quotation.PurchaseRequestItem.ProductId;
                var supplierProduct = supplier.SupplierProducts.FirstOrDefault(sp => sp.ProductId == productId);
                if (supplierProduct is null)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "El proveedor no está vinculado al producto.",
                        "ERP:SUPPLIER_PRODUCT_NOT_LINKED");
                }

                quotation.SupplierId = supplier.Id;
                quotation.Supplier = supplier;
                quotation.SupplierProductId = supplierProduct.Id;
                quotation.SupplierProduct = supplierProduct;
            }

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

            var paymentMethods = quotation.Supplier?.SupplierPaymentMethods ?? [];
            var paymentResolution = ResolvePaymentMethod(
                request.PaymentMethodType ?? quotation.PaymentMethodType,
                paymentMethods,
                request.PaymentMethodType.HasValue);

            if (paymentResolution.error is not null)
            {
                return _errorManager.ThrowBadRequest<bool>(paymentResolution.error, "ERP:PAYMENT_METHOD_REQUIRED");
            }

            quotation.PaymentMethodType = paymentResolution.method;

            var quantity = quotation.PurchaseRequestItem?.Quantity ?? 0;
            var quoteDate = quotation.QuoteDate == default
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : quotation.QuoteDate;

            if (quotation.SupplierProduct is not null && quantity > 0)
            {
                var rates = await purchaseTaxService.GetActiveRatesAsync(cancellationToken);
                quotation.PriceUnit = purchaseTaxService.ResolveUnitPrice(quotation.SupplierProduct, quantity, quoteDate);
                var subtotal = Math.Round(quantity * quotation.PriceUnit, 2, MidpointRounding.AwayFromZero);
                quotation.PriceTotal = subtotal;
                quotation.Iva = purchaseTaxService.CalculateIva(
                    subtotal,
                    quotation.PurchaseRequestItem?.Product?.IsTaxExempt ?? false,
                    rates);
            }

            if (request.Attachments is not null)
            {
                var (json, attachmentError) = await quotationAttachmentService.BuildAdditionalDataAsync(
                    request.Attachments,
                    quotation.AdditionalData,
                    cancellationToken);

                if (attachmentError is not null)
                {
                    return _errorManager.ThrowBadRequest<bool>(attachmentError, "ERP:INVALID_QUOTATION_ATTACHMENT");
                }

                quotation.AdditionalData = json;
            }

            await _unitOfWork.Quotations.UpdateAsync(quotation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cotización actualizada con éxito");
            return true;
        }

        private static (PaymentMethodType method, string? error) ResolvePaymentMethod(
            PaymentMethodType? requested,
            IEnumerable<SupplierPaymentMethod> methods,
            bool forceRequested)
        {
            var active = methods
                .Where(m => m.IsActive && m.DeletedAt == null)
                .Select(m => m.PaymentMethodType)
                .Distinct()
                .ToList();

            if (active.Count == 0)
            {
                return (default, "El proveedor no tiene métodos de pago activos.");
            }

            if (forceRequested && requested.HasValue)
            {
                if (!active.Contains(requested.Value))
                {
                    return (default, "El método de pago seleccionado no pertenece al proveedor.");
                }

                return (requested.Value, null);
            }

            if (requested.HasValue && active.Contains(requested.Value))
            {
                return (requested.Value, null);
            }

            if (active.Count == 1)
            {
                return (active[0], null);
            }

            if (requested.HasValue)
            {
                return (default, "El método de pago seleccionado no pertenece al proveedor.");
            }

            return (default, "Debe seleccionar un método de pago porque el proveedor tiene varios.");
        }
    }
}
