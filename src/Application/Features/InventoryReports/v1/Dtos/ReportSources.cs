using InventoryOutputEntity = ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs;
using ERP.Core.Database.Domain.Entities.Inventory;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class OutputLineEquipmentSource
{
    public InventoryOutputEntity Output { get; set; } = default!;
    public SupplyRequestItem? Item { get; set; }
    public bool IsFuel { get; set; }
}

public class OutputDetailByEquipmentSource
{
    public InventoryOutputEntity Output { get; set; } = default!;
    public SupplyRequestItem? Item { get; set; }
    public bool IsFuel { get; set; }
}