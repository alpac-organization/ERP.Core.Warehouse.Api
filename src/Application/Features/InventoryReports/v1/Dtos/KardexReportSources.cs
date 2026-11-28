using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Warehouse;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public class KardexMovementSource
{
    public Kardex Kardex { get; set; } = default!;
    public string MovementType { get; set; } = null!;
    public string? RecordNumber { get; set; }
    public ReferenceType Document { get; set; }
    public string? DocumentCode { get; set; }
    public decimal BalanceUnits { get; set; }
    public decimal AverageCost { get; set; }
    public decimal KardexBalance { get; set; }
}

public class KardexProductSource
{
    public Product Product { get; set; } = default!;
    public string? WarehouseName { get; set; }
    public decimal InitialKardexBalance { get; set; }
    public decimal InitialInventory { get; set; }
    public decimal InitialAverageCost { get; set; }
    public List<KardexMovementSource> Rows { get; set; } = [];
    public KardexProductSummaryDto Summary { get; set; } = new();
}
