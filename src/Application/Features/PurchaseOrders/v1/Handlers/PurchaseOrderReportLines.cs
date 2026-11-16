using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Models;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    internal static class PurchaseOrderReportLines
    {
        public static List<Quotation> AcceptedQuotations(PurchaseOrder purchaseOrder)
        {
            return purchaseOrder.PurchaseRequest.PurchaseRequestItems
                .SelectMany(item => item.Quotations)
                .Where(quotation => quotation.IsActive && quotation.DeletedAt == null && quotation.IsAcceptedForPurchase)
                .Where(quotation => !purchaseOrder.SupplierId.HasValue || quotation.SupplierId == purchaseOrder.SupplierId)
                .ToList();
        }

        public static Currency ResolveDocumentCurrency(IReadOnlyList<Quotation> quotations)
        {
            var currencies = quotations
                .Select(LineCurrency)
                .Distinct()
                .ToList();

            if (currencies.Count > 1)
            {
                return Currency.NIO;
            }

            return currencies.Count == 0 ? Currency.NIO : currencies[0];
        }

        public static List<PurchaseOrderReportItemDto> BuildItems(
            PurchaseOrder purchaseOrder,
            IReadOnlyList<Quotation> supplierQuotations,
            Currency documentCurrency,
            decimal exchangeRate,
            PurchaseTaxRates rates,
            IPurchaseTaxService purchaseTaxService)
        {
            var quotationsByRequestItem = supplierQuotations
                .GroupBy(quotation => quotation.PurchaseRequestItemId)
                .ToDictionary(group => group.Key, group => group.First());

            var requestItems = purchaseOrder.PurchaseRequest.PurchaseRequestItems
                .ToDictionary(item => item.Id);

            return purchaseOrder.PurchaseOrderItems
                .Select(orderItem =>
                {
                    Quotation? quotation = null;
                    PurchaseRequestItem? requestItem = null;
                    if (orderItem.PurchaseRequestItemId.HasValue)
                    {
                        quotationsByRequestItem.TryGetValue(orderItem.PurchaseRequestItemId.Value, out quotation);
                        requestItems.TryGetValue(orderItem.PurchaseRequestItemId.Value, out requestItem);
                    }

                    var quantity = orderItem.Quantity;
                    var unitPrice = orderItem.UnitPrice;
                    var priceTotal = quotation?.PriceTotal ?? quantity * unitPrice;
                    var iva = quotation?.Iva ?? 0m;
                    var lineCurrency = quotation is null ? Currency.NIO : LineCurrency(quotation);

                    if (documentCurrency == Currency.NIO && lineCurrency == Currency.USD)
                    {
                        unitPrice = RoundMoney(unitPrice * exchangeRate);
                        priceTotal = RoundMoney(unitPrice * quantity);
                        var isTaxExempt = orderItem.Product?.IsTaxExempt
                            ?? requestItem?.Product?.IsTaxExempt
                            ?? false;
                        iva = RoundMoney(purchaseTaxService.CalculateIva(priceTotal, isTaxExempt, rates));
                    }

                    return new PurchaseOrderReportItemDto
                    {
                        PurchaseOrderItemId = orderItem.Id,
                        PurchaseRequestItemId = orderItem.PurchaseRequestItemId,
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.Product?.ProductName,
                        ProductCode = orderItem.Product?.Code,
                        Quantity = quantity,
                        UnitMeasure = requestItem?.UnitMeasure?.Name ?? requestItem?.UnitMeasure?.Symbol,
                        UnitPrice = unitPrice,
                        PriceTotal = priceTotal,
                        Iva = iva
                    };
                })
                .ToList();
        }

        public static PurchaseRetentionResult CalculateSupplierRetention(
            decimal subtotal,
            Currency currency,
            Supplier? supplier,
            PurchaseTaxRates rates,
            IPurchaseTaxService purchaseTaxService)
        {
            var details = supplier?.SupplierDetails;

            return purchaseTaxService.CalculateRetentions(
                subtotal,
                currency,
                details?.SupplierType,
                details?.IsTaxExempt ?? false,
                details?.ApplyIrRetention ?? false,
                details?.ApplyMunicipalRetention ?? false,
                rates);
        }

        private static Currency LineCurrency(Quotation quotation)
        {
            return quotation.SupplierProduct?.Currency ?? Currency.NIO;
        }

        private static decimal RoundMoney(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
