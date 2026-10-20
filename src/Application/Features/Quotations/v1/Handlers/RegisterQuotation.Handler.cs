using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
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
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        ILogger<RegisterQuotationHandler> logger,
        IPurchaseTaxService purchaseTaxService,
        ISupplierProductLinkService supplierProductLinkService,
        IQuotationAttachmentService quotationAttachmentService)
        : BaseValidatorHandler<RegisterQuotationCommand, bool>(unitOfWork, errorManager)
    {
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

            logger.LogInformation("Iniciando registro de cotizaciones.");

            var itemIds = request.QuotationItems.Select(q => q.PurchaseRequestItemId).Distinct().ToList();
            var supplierIds = request.QuotationItems.Select(q => q.SupplierId).Distinct().ToList();

            var items = await _unitOfWork.PurchaseRequestItems.Entities
                .Include(i => i.Product)
                .Where(i => itemIds.Contains(i.Id) && i.DeletedAt == null)
                .ToDictionaryAsync(i => i.Id, cancellationToken);

            var suppliers = await _unitOfWork.Suppliers.Entities
                .Include(s => s.SupplierDetails)
                .Include(s => s.SupplierPaymentMethods.Where(spm => spm.IsActive && spm.DeletedAt == null))
                .Include(s => s.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                    .ThenInclude(sp => sp.TierPrices)
                .AsSplitQuery()
                .Where(s => supplierIds.Contains(s.Id) && s.DeletedAt == null)
                .ToDictionaryAsync(s => s.Id, cancellationToken);

            var rates = await purchaseTaxService.GetActiveRatesAsync(cancellationToken);
            var quoteDate = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var quotation in request.QuotationItems)
            {
                if (!items.TryGetValue(quotation.PurchaseRequestItemId, out var item))
                {
                    return _errorManager.ThrowNotFound<bool>(
                        "No se encontró el ítem de la solicitud de compra",
                        "ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND");
                }

                if (item.Product is null)
                {
                    return _errorManager.ThrowNotFound<bool>(
                        "No se encontró el producto del ítem de la solicitud",
                        "ERP:PRODUCT_NOT_FOUND");
                }

                if (!suppliers.TryGetValue(quotation.SupplierId, out var supplier))
                {
                    return _errorManager.ThrowNotFound<bool>(
                        "No se encontró el proveedor",
                        "ERP:SUPPLIER_NOT_FOUND");
                }

                var linkResult = await ResolveOrCreateSupplierProductAsync(
                    quotation,
                    item,
                    supplier,
                    quoteDate,
                    cancellationToken);

                if (linkResult.ErrorCode is not null)
                {
                    return _errorManager.ThrowBadRequest<bool>(linkResult.ErrorMessage!, linkResult.ErrorCode);
                }

                var supplierProduct = linkResult.SupplierProduct!;

                var paymentMethod = ResolvePaymentMethod(quotation.PaymentMethodType, supplier.SupplierPaymentMethods);
                if (paymentMethod.error is not null)
                {
                    return _errorManager.ThrowBadRequest<bool>(paymentMethod.error, "ERP:PAYMENT_METHOD_REQUIRED");
                }

                quotation.PaymentMethodType = paymentMethod.method;

                var priceUnit = supplierProductLinkService.ResolveQuotationUnitPrice(
                    supplierProduct,
                    item.Quantity,
                    quoteDate,
                    quotation.PriceUnit,
                    purchaseTaxService);

                if (priceUnit is null or <= 0)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "Debe indicar el precio unitario porque el vínculo no tiene precio vigente.",
                        "ERP:PRICE_UNIT_REQUIRED");
                }

                var subtotal = Math.Round(item.Quantity * priceUnit.Value, 2, MidpointRounding.AwayFromZero);
                var ivaAmount = purchaseTaxService.CalculateIva(
                    subtotal,
                    item.Product.IsTaxExempt,
                    rates);
                var retention = CalculateRetention(supplier, supplierProduct.Currency, subtotal, rates);

                var (additionalData, attachmentError) = await quotationAttachmentService.BuildAdditionalDataAsync(
                    quotation.Attachments,
                    existingJson: null,
                    cancellationToken);

                if (attachmentError is not null)
                {
                    return _errorManager.ThrowBadRequest<bool>(attachmentError, "ERP:INVALID_QUOTATION_ATTACHMENT");
                }

                var quotationEntity = QuotationsMapper.ToQuotationsEntity(
                    quotation,
                    priceUnit.Value,
                    subtotal,
                    ivaAmount,
                    retention.IrAmount,
                    retention.ImiAmount,
                    supplierProduct.Id,
                    additionalData);

                quotationEntity.SupplierProduct = supplierProduct;

                if (!item.HasQuotation)
                {
                    item.HasQuotation = true;
                    await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);
                }

                await _unitOfWork.Quotations.RegisterQuotation(quotationEntity);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cotización agregada con éxito");
            return true;
        }

        private async Task<(SupplierProduct? SupplierProduct, string? ErrorMessage, string? ErrorCode)> ResolveOrCreateSupplierProductAsync(
            QuotationItem quotation,
            PurchaseRequestItem item,
            Supplier supplier,
            DateOnly quoteDate,
            CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Products.Entities
                .Include(p => p.SupplierProducts)
                    .ThenInclude(sp => sp.TierPrices)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

            var supplierProduct = product?.SupplierProducts
                .FirstOrDefault(sp => sp.SupplierId == supplier.Id);

            if (supplierProduct is not null)
            {
                if (!supplierProduct.IsActive || supplierProduct.DeletedAt is not null)
                {
                    supplierProduct.IsActive = true;
                    supplierProduct.DeletedAt = null;
                    supplierProduct.UnitPrice = quotation.PriceUnit ?? supplierProduct.UnitPrice;
                    supplierProduct.Currency = quotation.Currency ?? supplierProduct.Currency;
                    supplierProduct.UnitMeasureId = item.UnitMeasureId != Guid.Empty
                        ? item.UnitMeasureId
                        : item.Product.UnitMeasureId;
                    supplierProduct.LastPriceUpdate = DateTime.UtcNow;

                }

                return (supplierProduct, null, null);
            }

            if (!quotation.CreateSupplierProductIfMissing)
            {
                return (
                    null,
                    "El proveedor no está vinculado al producto.",
                    "ERP:SUPPLIER_PRODUCT_NOT_FOUND");
            }

            if (quotation.PriceUnit is null or <= 0)
            {
                return (
                    null,
                    "Debe indicar el precio unitario para vincular el proveedor al producto.",
                    "ERP:PRICE_UNIT_REQUIRED");
            }

            var unitMeasureId = item.UnitMeasureId != Guid.Empty
                ? item.UnitMeasureId
                : item.Product.UnitMeasureId;

            if (unitMeasureId == Guid.Empty)
            {
                return (
                    null,
                    "No se pudo determinar la unidad de medida para el vínculo proveedor–producto.",
                    "ERP:UNIT_MEASURE_REQUIRED");
            }

            var created = supplierProductLinkService.CreateLink(
                productId: item.ProductId,
                supplierId: supplier.Id,
                unitMeasureId: unitMeasureId,
                unitPrice: quotation.PriceUnit.Value,
                currency: quotation.Currency ?? Currency.NIO,
                preferentialPrice: quotation.PreferentialPrice,
                validFrom: quoteDate);

            supplier.SupplierProducts.Add(created);

            return (created, null, null);
        }

        private PurchaseRetentionResult CalculateRetention(
            Supplier supplier,
            Currency currency,
            decimal subtotal,
            PurchaseTaxRates rates)
        {
            var details = supplier.SupplierDetails;

            return purchaseTaxService.CalculateRetentions(
                subtotal,
                currency,
                details?.SupplierType,
                details?.IsTaxExempt ?? false,
                details?.ApplyIrRetention ?? false,
                details?.ApplyMunicipalRetention ?? false,
                rates);
        }

        private static (PaymentMethodType method, string? error) ResolvePaymentMethod(
            PaymentMethodType? requested,
            IEnumerable<SupplierPaymentMethod> methods)
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

            if (requested.HasValue)
            {
                if (!active.Contains(requested.Value))
                {
                    return (default, "El método de pago seleccionado no pertenece al proveedor.");
                }

                return (requested.Value, null);
            }

            if (active.Count == 1)
            {
                return (active[0], null);
            }

            return (default, "Debe seleccionar un método de pago porque el proveedor tiene varios.");
        }
    }
}
