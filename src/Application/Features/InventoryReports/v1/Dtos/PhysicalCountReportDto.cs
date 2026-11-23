namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class PhysicalCountReportDto
{
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }

    public int TotalProducts { get; set; }
    public decimal TotalSystemStock { get; set; }
    public decimal TotalPhysicalStock { get; set; }
    public decimal TotalDifference { get; set; }
    public int ShortageCount { get; set; }
    public int OverageCount { get; set; }
    public int MatchedCount { get; set; }

    public List<PhysicalCountReportItemDto> Items { get; set; } = [];
}

public class PhysicalCountReportItemDto
{
    public string? BranchName { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public string? UnitMeasure { get; set; }
    public decimal InventoryStock { get; set; }
    public decimal? PhysicalStock { get; set; }
    public decimal? Difference { get; set; }
    public string? Observations { get; set; }
}