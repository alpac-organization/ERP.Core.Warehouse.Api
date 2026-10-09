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
        IQuotationAttachmentService quotationAttachmentService)
        : BaseValidatorHandler<RegisterQuotationCommand, bool>(unitOfWork, errorManager)
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
                return errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            logger.LogInformation("Iniciando registro de cotizaciones.");

            var itemIds = request.QuotationItems.Select(q => q.PurchaseRequestItemId).Distinct().ToList();
            var supplierIds = request.QuotationItems.Select(q => q.SupplierId).Distinct().ToList();

            var items = await unitOfWork.PurchaseRequestItems.Entities
                .Include(i => i.Product)
                .Where(i => itemIds.Contains(i.Id) && i.DeletedAt == null)
                .ToDictionaryAsync(i => i.Id, cancellationToken);

            var suppliers = await unitOfWork.Suppliers.Entities
                .Include(s => s.SupplierPaymentMethods.Where(spm => spm.IsActive && spm.DeletedAt == null))
                .Include(s => s.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                    .ThenInclude(sp => sp.TierPrices)
                .Where(s => supplierIds.Contains(s.Id) && s.DeletedAt == null)
                .ToDictionaryAsync(s => s.Id, cancellationToken);

            var rates = await purchaseTaxService.GetActiveRatesAsync(cancellationToken);
            var quoteDate = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var quotation in request.QuotationItems)
            {
                if (!items.TryGetValue(quotation.PurchaseRequestItemId, out var item))
                {
                    return errorManager.ThrowNotFound<bool>(
                        "No se encontró el ítem de la solicitud de compra",
                        "ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND");
                }

                if (!suppliers.TryGetValue(quotation.SupplierId, out var supplier))
                {
                    return errorManager.ThrowNotFound<bool>(
                        "No se encontró el proveedor",
                        "ERP:SUPPLIER_NOT_FOUND");
                }

                var supplierProduct = supplier.SupplierProducts
                    .FirstOrDefault(sp => sp.ProductId == item.ProductId);

                if (supplierProduct is null)
                {
                    return errorManager.ThrowBadRequest<bool>(
                        "El proveedor no está vinculado al producto. Agregue la relación desde la solicitud de compra.",
                        "ERP:SUPPLIER_PRODUCT_NOT_LINKED");
                }

                var paymentMethod = ResolvePaymentMethod(quotation.PaymentMethodType, supplier.SupplierPaymentMethods);
                if (paymentMethod.error is not null)
                {
                    return errorManager.ThrowBadRequest<bool>(paymentMethod.error, "ERP:PAYMENT_METHOD_REQUIRED");
                }

                quotation.PaymentMethodType = paymentMethod.method;

                var priceUnit = purchaseTaxService.ResolveUnitPrice(supplierProduct, item.Quantity, quoteDate);
                var subtotal = Math.Round(item.Quantity * priceUnit, 2, MidpointRounding.AwayFromZero);
                var ivaAmount = purchaseTaxService.CalculateIva(
                    subtotal,
                    item.Product?.IsTaxExempt ?? false,
                    rates);

                var (additionalData, attachmentError) = await quotationAttachmentService.BuildAdditionalDataAsync(
                    quotation.Attachments,
                    existingJson: null,
                    cancellationToken);

                if (attachmentError is not null)
                {
                    return errorManager.ThrowBadRequest<bool>(attachmentError, "ERP:INVALID_QUOTATION_ATTACHMENT");
                }

                var quotationEntity = QuotationsMapper.ToQuotationsEntity(
                    quotation,
                    priceUnit,
                    subtotal,
                    ivaAmount,
                    supplierProduct.Id,
                    additionalData);

                if (!item.HasQuotation)
                {
                    item.HasQuotation = true;
                    await unitOfWork.PurchaseRequestItems.UpdateAsync(item);
                }

                await unitOfWork.Quotations.RegisterQuotation(quotationEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cotización agregada con éxito");
            return true;
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
