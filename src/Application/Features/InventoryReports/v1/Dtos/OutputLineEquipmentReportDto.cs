namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class OutputLineEquipmentReportDto
{
    public string? OutputCode { get; set; }
    public string? RequestCode { get; set; }
    public DateTime Date { get; set; }
    public string? Supply { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
    public decimal TotalCost { get; set; }
    public string? CostCenter { get; set; }
    public string? ServiceOrderNumber { get; set; }
    public string? Customer { get; set; }
}