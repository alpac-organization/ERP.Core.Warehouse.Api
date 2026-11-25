namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public abstract class OutputReportBaseDto
{
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
