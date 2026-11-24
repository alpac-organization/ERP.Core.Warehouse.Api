using ERP.Core.Database.Domain.Entities.Inventory;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderEntryAdditionalData : InventoryEntryAdditionalData
    {
        public Guid? PurchaseOrderId { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public List<PurchaseOrderEntryAdditionalItem> Items { get; set; } = [];
    }

    public class PurchaseOrderEntryAdditionalItem
    {
        public Guid? PurchaseOrderItemId { get; set; }
        public Guid? PurchaseRequestItemId { get; set; }
        public Guid? QuotationId { get; set; }

        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Guid UnitMeasureId { get; set; }
        public string? UnitMeasureName { get; set; }
        public string? UnitMeasureSymbol { get; set; }

        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public Guid? CostCenterId { get; set; }
        public string? CostCenterName { get; set; }
        public string? CostCenterCode { get; set; }
        public string? CostCenterDescription { get; set; }
        public int? CostCenterCoilCode { get; set; }
    }
}
