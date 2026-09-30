namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

public class GetAssignmentMachineryDto
{
    public Guid AssignmentMachineryId { get; set; }
    public Guid MachineryId { get; set; }
    public string? Concept { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public Guid AssignmentOperationalId { get; set; }

}