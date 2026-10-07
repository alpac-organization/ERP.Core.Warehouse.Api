using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public static class PurchaseTaxPricingService
    {
        public const decimal RetentionThresholdNio = 1000m;

        public static async Task<decimal?> GetActiveTaxValueAsync(
            IUnitOfWork unitOfWork,
            TaxType taxType,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            return await unitOfWork.ValidityDeductions.Entities
                .AsNoTracking()
                .Where(v => v.Type == taxType)
                .Where(v => v.Status)
                .Where(v => v.DeletedAt == null)
                .Where(v => v.StartDate <= now)
                .Where(v => v.EndDate == null || v.EndDate >= now)
                .OrderByDescending(v => v.StartDate)
                .Select(v => (decimal?)v.Value)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public static decimal ResolveUnitPrice(SupplierProduct supplierProduct, int quantity, DateOnly quoteDate)
        {
            var preferential = supplierProduct.TierPrices
                .Where(t => t.DeletedAt == null)
                .Where(t => quantity >= t.MinQuantity)
                .Where(t => t.ValidFrom <= quoteDate)
                .Where(t => t.ValidTo == null || t.ValidTo >= quoteDate)
                .OrderByDescending(t => t.MinQuantity)
                .ThenByDescending(t => t.ValidFrom)
                .Select(t => (decimal?)t.PreferentialPrice)
                .FirstOrDefault();

            if (preferential.HasValue)
            {
                return preferential.Value;
            }

            return supplierProduct.UnitPrice;
        }

        public static decimal CalculateIvaAmount(decimal subtotal, bool isTaxExempt, decimal? ivaRatePercent)
        {
            if (isTaxExempt || !ivaRatePercent.HasValue || ivaRatePercent.Value <= 0)
            {
                return 0m;
            }

            return Math.Round(subtotal * (ivaRatePercent.Value / 100m), 2, MidpointRounding.AwayFromZero);
        }

        public static bool ShouldApplyRetentions(decimal totalNio, bool supplierIsTaxExempt)
        {
            return !supplierIsTaxExempt && totalNio >= RetentionThresholdNio;
        }

        public static decimal CalculateRetentionAmount(decimal baseAmount, decimal? ratePercent)
        {
            if (!ratePercent.HasValue || ratePercent.Value <= 0 || baseAmount <= 0)
            {
                return 0m;
            }

            return Math.Round(baseAmount * (ratePercent.Value / 100m), 2, MidpointRounding.AwayFromZero);
        }
    }

    public class PurchaseOrderTaxMetadata
    {
        public decimal ExchangeRate { get; set; }
        public decimal TotalNio { get; set; }
        public decimal ImiAmount { get; set; }
        public decimal IrAmount { get; set; }
        public decimal ImiRate { get; set; }
        public decimal IrRate { get; set; }
        public bool RetentionsApplied { get; set; }
    }
}
