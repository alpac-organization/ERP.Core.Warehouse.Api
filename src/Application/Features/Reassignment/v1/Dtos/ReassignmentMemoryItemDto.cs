namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

public class ReassignmentMemoryItemDto
{
    public Guid MemoryItemId { get; set; }
    public Guid SessionId { get; set; }
    public Guid StockId { get; set; }
    public Guid? TargetRackPositionId { get; set; }
    public Guid? TargetLotPositionId { get; set; }
    public DateOnly LiftedAtDate { get; set; }
    public TimeOnly LiftedAtTime { get; set; }
}
