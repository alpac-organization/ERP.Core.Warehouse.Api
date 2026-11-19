namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class StockDailyReportDto
{
    public DateTime GeneratedAt { get; set; }
    public List<StockDailyBranchDto> Branches { get; set; } = [];
}

public class StockDailyBranchDto
{
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public List<StockDailyAreaDto> Areas { get; set; } = [];
}

public class StockDailyAreaDto
{
    public Guid AreaId { get; set; }
    public string? AreaName { get; set; }
    public List<StockDailyCostCenterDto> CostCenters { get; set; } = [];
}

public class StockDailyCostCenterDto
{
    public Guid CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public List<StockDailyUsageTypeDto> UsageTypes { get; set; } = [];
}

public class StockDailyUsageTypeDto
{
    public string? UsageType { get; set; }
    public List<StockDailyProductDto> Products { get; set; } = [];
}

public class StockDailyProductDto
{
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public decimal InventoryStock { get; set; }
    public string? UnitMeasure { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }
    public string? StockStatus { get; set; }
}