using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class OutputConsolidatedByEquipmentReportDto : OutputReportBaseDto
{
    public List<OutputConsolidatedCategoryDto> Categories { get; set; } = [];
}

public class OutputConsolidatedCategoryDto
{
    public string? CategoryName { get; set; }
    public List<OutputConsolidatedItemDto> Items { get; set; } = [];

    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

public class OutputConsolidatedItemDto
{
    public string? Equipment { get; set; }
    public string? StockCode { get; set; }
    public string? Supply { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalCost { get; set; }
    public string? CostCenter { get; set; }
    public string? Customer { get; set; }
}
