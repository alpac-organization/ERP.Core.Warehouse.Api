using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos
{
    public class InventoryEntryDetailDto : InventoryEntryDto
    {
        public EntryType EntryType { get; set; }
        public string? Observations { get; set; }
        public string? AdditionalData { get; set; }
        public DateTime CreatedAt { get; set; }

        public WorkAreaInformation? WorkAreaInformation { get; set; }
        public PurchaseRequestDto? PurchaseRequestInformation { get; set; }
        public List<InventoryEntryPurchaseRequestItemDto> PurchaseRequestItems { get; set; } = [];

        public Guid? PurchaseOrderId { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public List<InventoryEntryItemDetailDto> Items { get; set; } = [];
    }

    public class InventoryEntryItemDetailDto
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
    }

    public class InventoryEntryPurchaseRequestItemDto
    {
        public Guid PurchaseRequestItemId { get; set; }
        public int Quantity { get; set; }
        public int? QuantityUnit { get; set; }
        public string? Description { get; set; }
        public string? Justification { get; set; }

        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? UnitMeasureName { get; set; }
        public string? UnitMeasureSymbol { get; set; }
    }
}
