namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class InventoryTurnoverReportDto
{
    public string? Category { get; set; }
    public string? ProductName { get; set; }
    public string? StockCode { get; set; }
    public decimal AvailableUnits { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPurchases { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public decimal TotalDispatches { get; set; }
    public decimal PurchaseUnits { get; set; }
    public DateTime? LastDispatchDate { get; set; }
    public decimal DispatchedUnits { get; set; }
    public decimal HistoricalMinCost { get; set; }
    public decimal HistoricalMaxCost { get; set; }
    public decimal MinDispatchedUnits { get; set; }
    public decimal MaxDispatchedUnits { get; set; }
    public decimal ConfiguredMinStock { get; set; }
    public decimal? ConfiguredMaxStock { get; set; }
    public decimal MonthlyRotationIndex { get; set; }
    public decimal AnnualRotationIndex { get; set; }
}