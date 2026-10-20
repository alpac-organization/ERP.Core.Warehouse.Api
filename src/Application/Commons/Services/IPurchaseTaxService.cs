using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public interface IPurchaseTaxService
    {
        Task<PurchaseTaxRates> GetActiveRatesAsync(CancellationToken cancellationToken);

        decimal ResolveUnitPrice(SupplierProduct supplierProduct, int quantity, DateOnly quoteDate);

        decimal CalculateIva(decimal subtotal, bool isTaxExempt, PurchaseTaxRates rates);

        /// <summary>
        /// Calcula IR e IMI de una cotización sobre su subtotal.
        /// </summary>
        /// <param name="subtotal">Subtotal de la cotización, sin IVA.</param>
        /// <param name="currency">Moneda del vínculo producto–proveedor.</param>
        /// <param name="supplierType">Tipo de proveedor. Internacional usa la tasa de IR internacional.</param>
        /// <param name="supplierIsTaxExempt">Si es verdadero, ambas retenciones quedan en cero.</param>
        /// <param name="applyIrRetention">Aplica IR solo cuando el proveedor tiene la bandera activa.</param>
        /// <param name="applyMunicipalRetention">Aplica IMI solo cuando el proveedor tiene la bandera activa.</param>
        /// <param name="rates">Tasas vigentes de IVA, IR, IMI y tipo de cambio.</param>
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
