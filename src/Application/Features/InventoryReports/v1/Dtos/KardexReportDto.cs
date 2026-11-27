using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class KardexReportDto : InventoryReportBaseDto
{
    public string? CompanyName { get; set; }
    public List<KardexProductReportDto> Products { get; set; } = [];
}

public class KardexProductReportDto
{
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? CategoryName { get; set; }
    public string? WarehouseName { get; set; }
    public string? UnitMeasureName { get; set; }
    public string? UnitMeasureSymbol { get; set; }
    public decimal InitialKardexBalance { get; set; }
    public decimal InitialInventory { get; set; }
    public decimal InitialAverageCost { get; set; }
    public List<KardexMovementRowDto> Rows { get; set; } = [];
    public KardexProductSummaryDto Summary { get; set; } = new();
}

public class KardexMovementRowDto
{
    public string MovementType { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? RecordNumber { get; set; }
    public ReferenceType Document { get; set; }
    public decimal EntryUnits { get; set; }
    public decimal ExitUnits { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalEntryCost { get; set; }
    public decimal TotalExitCost { get; set; }
    public decimal BalanceUnits { get; set; }
    public decimal AverageCost { get; set; }
    public decimal KardexBalance { get; set; }
}

public class KardexProductSummaryDto
{
    public int RowCount { get; set; }
    public decimal TotalEntries { get; set; }
    public decimal TotalExits { get; set; }
    public decimal TotalEntryCost { get; set; }
    public decimal TotalExitCost { get; set; }
    public decimal FinalUnits { get; set; }
    public decimal FinalAverageCost { get; set; }
    public decimal FinalKardexBalance { get; set; }
}
