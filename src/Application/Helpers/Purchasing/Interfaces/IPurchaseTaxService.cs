using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Models;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces
{
    public interface IPurchaseTaxService
    {
        Task<PurchaseTaxRates> GetActiveRatesAsync(CancellationToken cancellationToken);

        decimal ResolveUnitPrice(SupplierProduct supplierProduct, int quantity, DateOnly quoteDate);

        decimal CalculateIva(decimal subtotal, bool isTaxExempt, PurchaseTaxRates rates);

        PurchaseRetentionResult CalculateRetentions(
            decimal subtotal,
            Currency currency,
            SupplierType? supplierType,
            bool supplierIsTaxExempt,
            bool applyIrRetention,
            bool applyMunicipalRetention,
            PurchaseTaxRates rates);
    }
}
