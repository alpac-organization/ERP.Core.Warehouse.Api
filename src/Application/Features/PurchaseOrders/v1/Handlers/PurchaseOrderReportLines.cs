using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

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

        public static List<PurchaseOrderReportItemDto> BuildItems(PurchaseOrder purchaseOrder, IReadOnlyList<Quotation> supplierQuotations)
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
                        PriceTotal = quotation?.PriceTotal ?? quantity * unitPrice,
                        Iva = quotation?.Iva ?? 0m
                    };
                })
                .ToList();
        }

        // aqui Obtiene la moneda única de las cotizaciones.  
        public static bool TryResolveCurrency(IReadOnlyList<Quotation> quotations, out Currency currency)
        {
            var currencies = quotations
                .Select(quotation => quotation.SupplierProduct?.Currency ?? Currency.NIO)
                .Distinct()
                .ToList();

            if (currencies.Count > 1)
            {
                currency = default;
                return false;
            }

            currency = currencies.Count == 0 ? Currency.NIO : currencies[0];
            return true;
        }
    }
}
