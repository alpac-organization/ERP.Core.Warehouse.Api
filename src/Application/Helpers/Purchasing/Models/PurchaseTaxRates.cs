using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Models
{
    public sealed record PurchaseTaxRates(
        decimal? ExchangeRate,
        decimal? Iva,
        decimal? Imi,
        decimal? Ir,
        decimal? IrSupplierInternation);

    public class PurchaseRetentionResult
    {
        public decimal ExchangeRate { get; set; }

        public decimal TotalNio { get; set; }

        public decimal ImiAmount { get; set; }

        public decimal IrAmount { get; set; }

        public decimal ImiRate { get; set; }

        public decimal IrRate { get; set; }

        public bool RetentionsApplied { get; set; }

        public SupplierType? SupplierType { get; set; }

        public TaxType? IrTaxType { get; set; }
    }
}
