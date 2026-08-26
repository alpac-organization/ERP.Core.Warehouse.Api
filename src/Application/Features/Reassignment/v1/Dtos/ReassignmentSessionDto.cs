namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

public class ReassignmentSessionDto
{
    public Guid SessionId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Status { get; set; } = null!;
    public string CurrentOwnerUserId { get; set; } = null!;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}