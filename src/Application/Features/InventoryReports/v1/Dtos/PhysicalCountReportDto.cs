namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class PhysicalCountReportDto
{
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public decimal InventoryStock { get; set; }
    public decimal? PhysicalStock { get; set; }
    public decimal? Difference { get; set; }
    public string? Observations { get; set; }
}