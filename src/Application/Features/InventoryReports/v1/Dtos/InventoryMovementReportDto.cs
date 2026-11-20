namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class InventoryMovementPurchaseDto
{
    public DateTime Date { get; set; }
    public string? EntryCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Supplier { get; set; }
    public string? InvoiceReference { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Total { get; set; }
}

public class InventoryMovementExitDto
{
    public DateTime Date { get; set; }
    public string? ExitCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ServiceOrderNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Total { get; set; }
}

public class InventoryMovementReportDto
{
    public decimal? PurchaseMinCost { get; set; }
    public decimal? PurchaseMaxCost { get; set; }
    public decimal? ExitMaxCost { get; set; }
    public List<InventoryMovementPurchaseDto> Purchases { get; set; } = [];
    public List<InventoryMovementExitDto> Exits { get; set; } = [];
}