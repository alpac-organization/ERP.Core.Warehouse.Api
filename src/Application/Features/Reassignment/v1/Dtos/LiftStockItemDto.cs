namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

public class LiftStockItemDto
{
    public Guid StockId { get; set; }
    public Guid? TargetRackPositionId { get; set; }
    public Guid? TargetLotPositionId { get; set; }
}
