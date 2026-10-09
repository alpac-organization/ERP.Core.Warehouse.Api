using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public interface IPurchaseTaxService
    {
        Task<PurchaseTaxRates> GetActiveRatesAsync(CancellationToken cancellationToken);

        decimal ResolveUnitPrice(SupplierProduct supplierProduct, int quantity, DateOnly quoteDate);

        decimal CalculateIva(decimal subtotal, bool isTaxExempt, PurchaseTaxRates rates);

        PurchaseRetentionResult CalculateRetentions(
            decimal subtotal,
            decimal ivaAmount,
            Currency currency,
            SupplierType? supplierType,
            bool supplierIsTaxExempt,
            PurchaseTaxRates rates);
    }
}
