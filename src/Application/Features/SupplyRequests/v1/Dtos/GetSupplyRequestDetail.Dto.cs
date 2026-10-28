namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

public class GetSupplyRequestDetailDto : GetSupplyRequestDto
{
    public WorkAreaInformation WorkAreaInformation { get; set; } = default!;
    public CostCenterInformation CostCenterInformation { get; set; } = default!;
    public BranchInformation BranchInformation { get; set; } = default!;
    public WarehouseInformation WarehouseInformation { get; set; } = default!;
    public OperationalOrderInformation OperationalOrderInformation { get; set; } = default!;
    public List<ItemDetail> Items { get; set; } = [];

}

public class WorkAreaInformation
{
    public Guid WorkAreaId { get; set; }
    public string WorkAreaName { get; set; } = null!;
}

public class CostCenterInformation
{
    public Guid CostCenterId { get; set; }
    public string CostCenterName { get; set; } = null!;
}

public class BranchInformation
{
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = null!;
}

public class WarehouseInformation
{
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = null!;
}

public class OperationalOrderInformation
{
    public Guid OperationalOrderId { get; set; }
    public string OperationalOrderCode { get; set; } = null!;
}
public class ItemDetail
{
    public Guid SupplyRequestItemId { get; set; }
    public Guid ProductStockId { get; set; }
    public string ProductStockName { get; set; } = null!;
    public decimal Amount { get; set; }
}