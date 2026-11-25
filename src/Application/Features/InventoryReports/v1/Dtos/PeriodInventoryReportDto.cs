namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class PeriodInventoryReportDto
{
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public List<PeriodInventoryItemDto> Items { get; set; } = [];

    public decimal TotalInitialTotal { get; set; }
    public decimal TotalEntryTotalCost { get; set; }
    public decimal TotalExitTotalCost { get; set; }
    public decimal TotalFinalTotal { get; set; }
}

public class PeriodInventoryItemDto
{
    public string? StockCode { get; set; }
    public string? ProductName { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal InitialCost { get; set; }
    public decimal InitialTotal { get; set; }
    public decimal EntryQuantity { get; set; }
    public decimal EntryTotalCost { get; set; }
    public decimal ExitQuantity { get; set; }
    public decimal ExitTotalCost { get; set; }
    public decimal FinalQuantity { get; set; }
    public decimal FinalCost { get; set; }
    public decimal FinalTotal { get; set; }
}
