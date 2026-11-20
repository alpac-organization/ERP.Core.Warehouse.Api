namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class OutputDetailByEquipmentReportDto
{
    public string? OutputCode { get; set; }
    public string? RequestCode { get; set; }
    public string? Equipment { get; set; }
    public string? Category { get; set; }
    public string? StockCode { get; set; }
    public DateTime Date { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
    public decimal TotalCost { get; set; }
    public string? CostCenter { get; set; }
    public string? ServiceOrderNumber { get; set; }
    public string? Customer { get; set; }
}