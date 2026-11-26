using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class InventoryMovementReportDto : InventoryReportBaseDto
{
    public InventoryMovementTotalsDto TotalEntrada { get; set; } = new();
    public InventoryMovementTotalsDto MovimientoProducto { get; set; } = new();

    public List<InventoryMovementEntryDto> Entradas { get; set; } = [];
    public List<InventoryMovementExitDto> Salidas { get; set; } = [];
}

public class InventoryMovementTotalsDto
{
    public decimal Units { get; set; }
    public decimal Total { get; set; }
}

public class InventoryMovementEntryDto
{
    public DateTime Date { get; set; }
    public string? DocumentCode { get; set; }
    public string? StockCode { get; set; }
    public string? ProductName { get; set; }
    public string? Reference { get; set; }
    public decimal Units { get; set; }
    public decimal Cost { get; set; }
    public decimal Total { get; set; }
    public string? Supplier { get; set; }
    public decimal MinimumCost { get; set; }
    public decimal MaximumCost { get; set; }
}

public class InventoryMovementExitDto
{
    public DateTime Date { get; set; }
    public string? DocumentCode { get; set; }
    public string? StockCode { get; set; }
    public string? ProductName { get; set; }
    public string? Reference { get; set; }
    public decimal Units { get; set; }
    public decimal Cost { get; set; }
    public decimal Total { get; set; }
    public string? ServiceOrderCode { get; set; }
    public decimal MaximumCost { get; set; }
}
