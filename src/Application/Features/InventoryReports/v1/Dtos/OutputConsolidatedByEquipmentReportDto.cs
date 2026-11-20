namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class OutputConsolidatedByEquipmentReportDto
{
    public string? Equipment { get; set; }
    public string? Category { get; set; }
    public string? StockCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalCost { get; set; }
    public string? CostCenter { get; set; }
    public string? Customer { get; set; }
}