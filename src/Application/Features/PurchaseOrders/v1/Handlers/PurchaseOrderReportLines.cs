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

            return purchaseOrder.PurchaseOrderItems
                .Select(orderItem =>
                {
                    Quotation? quotation = null;
                    if (orderItem.PurchaseRequestItemId.HasValue)
                    {
                        quotationsByRequestItem.TryGetValue(orderItem.PurchaseRequestItemId.Value, out quotation);
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
                        UnitPrice = unitPrice,
                        PriceTotal = quotation?.PriceTotal ?? quantity * unitPrice,
                        Iva = quotation?.Iva ?? 0m
                    };
                })
                .ToList();
        }
    }
}
