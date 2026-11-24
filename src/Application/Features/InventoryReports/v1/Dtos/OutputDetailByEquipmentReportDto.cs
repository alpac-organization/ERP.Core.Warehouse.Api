namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class OutputDetailByEquipmentReportDto
{
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public List<OutputDetailByEquipmentItemDto> Items { get; set; } = [];

    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalTotalCost { get; set; }
}

public class OutputDetailByEquipmentItemDto
{
    public string? OutputCode { get; set; }
    public string? RequestCode { get; set; }
    public string? Equipment { get; set; }
    public string? Category { get; set; }
    public DateTime OutputDate { get; set; }
    public string? StockCode { get; set; }
    public string? Supply { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
    public decimal TotalCost { get; set; }
    public string? CostCenter { get; set; }
    public string? ServiceOrderCode { get; set; }
    public string? Customer { get; set; }
}
