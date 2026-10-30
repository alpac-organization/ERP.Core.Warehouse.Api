using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

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

public class ItemDetail
{
    public Guid SupplyRequestItemId { get; set; }
    public Guid ProductStockId { get; set; }
    public string ProductStockName { get; set; } = null!;
    public decimal Amount { get; set; }
}