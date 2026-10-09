using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public class PurchaseTaxService(IUnitOfWork unitOfWork) : IPurchaseTaxService
    {
        public const decimal RetentionThresholdNio = 1000m;

        private static readonly TaxType[] PurchasingTaxTypes =
        [
            TaxType.ExchangeRate,
            TaxType.Iva,
            TaxType.Imi,
            TaxType.Ir,
            TaxType.IrSupplierInternation
        ];

        public async Task<PurchaseTaxRates> GetActiveRatesAsync(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);

            var rows = await unitOfWork.ValidityDeductions.Entities
                .AsNoTracking()
                .Where(v => v.Status)
                .Where(v => v.DeletedAt == null)
                .Where(v => v.EndDate == null)
                .Where(v => PurchasingTaxTypes.Contains(v.Type))
                .Select(v => new { v.Type, v.Value, v.StartDate })
                .ToListAsync(cancellationToken);

            decimal? Pick(TaxType type) => rows
                .Where(r => r.Type == type)
                .OrderByDescending(r => r.StartDate)
                .Select(r => (decimal?)r.Value)
                .FirstOrDefault();

            return new PurchaseTaxRates(
                ExchangeRate: Pick(TaxType.ExchangeRate),
                Iva: Pick(TaxType.Iva),
                Imi: Pick(TaxType.Imi),
                Ir: Pick(TaxType.Ir),
                IrSupplierInternation: Pick(TaxType.IrSupplierInternation));
        }

        public decimal ResolveUnitPrice(SupplierProduct supplierProduct, int quantity, DateOnly quoteDate)
        {
            ArgumentNullException.ThrowIfNull(supplierProduct);

            var tiers = supplierProduct.TierPrices ?? [];

            var preferential = tiers
                .Where(t => t.DeletedAt == null)
                .Where(t => quantity >= t.MinQuantity)
                .Where(t => t.ValidFrom <= quoteDate)
                .Where(t => t.ValidTo == null || t.ValidTo >= quoteDate)
                .OrderByDescending(t => t.MinQuantity)
                .ThenByDescending(t => t.ValidFrom)
                .Select(t => (decimal?)t.PreferentialPrice)
                .FirstOrDefault();

            return preferential ?? supplierProduct.UnitPrice;
        }

        public decimal CalculateIva(decimal subtotal, bool isTaxExempt, PurchaseTaxRates rates)
        {
            ArgumentNullException.ThrowIfNull(rates);

            if (isTaxExempt || !rates.Iva.HasValue || rates.Iva.Value <= 0 || subtotal <= 0)
            {
                return 0m;
            }

            return subtotal * rates.Iva.Value / 100m;
        }

        public PurchaseRetentionResult CalculateRetentions(
            decimal subtotal,
            decimal ivaAmount,
            Currency currency,
            SupplierType? supplierType,
            bool supplierIsTaxExempt,
            PurchaseTaxRates rates)
        {
            ArgumentNullException.ThrowIfNull(rates);

            var exchangeRate = rates.ExchangeRate is > 0 ? rates.ExchangeRate.Value : 36.6243m;
            var total = subtotal + ivaAmount;
            var totalNio = currency == Currency.USD
                ? Math.Round(total * exchangeRate, 2, MidpointRounding.AwayFromZero)
                : total;

            var result = new PurchaseRetentionResult
            {
                ExchangeRate = exchangeRate,
                TotalNio = totalNio,
                SupplierType = supplierType,
                RetentionsApplied = false
            };

            if (supplierIsTaxExempt || totalNio < RetentionThresholdNio)
            {
                return result;
            }

            var isInternational = supplierType == SupplierType.International;
            var irRate = isInternational ? rates.IrSupplierInternation : rates.Ir;
            var irTaxType = isInternational ? TaxType.IrSupplierInternation : TaxType.Ir;

            var imiAmount = CalculatePercent(subtotal, rates.Imi);
            var irBase = currency == Currency.USD
                ? Math.Round(subtotal * exchangeRate, 2, MidpointRounding.AwayFromZero)
                : subtotal;
            var irAmount = CalculatePercent(irBase, irRate);

            result.ImiAmount = imiAmount;
            result.IrAmount = irAmount;
            result.ImiRate = rates.Imi ?? 0m;
            result.IrRate = irRate ?? 0m;
            result.IrTaxType = irTaxType;
            result.RetentionsApplied = imiAmount > 0 || irAmount > 0;

            return result;
        }

        private static decimal CalculatePercent(decimal baseAmount, decimal? ratePercent)
        {
            if (!ratePercent.HasValue || ratePercent.Value <= 0 || baseAmount <= 0)
            {
                return 0m;
            }

            return baseAmount * ratePercent.Value / 100m;
        }
    }
}
