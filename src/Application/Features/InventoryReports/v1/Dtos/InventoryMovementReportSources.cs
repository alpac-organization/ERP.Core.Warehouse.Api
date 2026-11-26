using ERP.Core.Database.Domain.Entities.Inventory;

using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class InventoryMovementEntrySource
{
    public Kardex Kardex { get; set; } = default!;
    public InventoryEntry? Entry { get; set; }
    public PurchaseOrderEntryAdditionalItem? AdditionalItem { get; set; }
    public string? RootSupplierName { get; set; }
    public decimal MinimumCost { get; set; }
    public decimal MaximumCost { get; set; }
}

public class InventoryMovementExitSource
{
    public Kardex Kardex { get; set; } = default!;
    public OutputReportSource OutputSource { get; set; } = default!;
    public decimal MaximumCost { get; set; }
}
