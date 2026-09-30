using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

public class GetAssignmentCollaboratorsDto
{
    public Guid AssignmentCollaboratorId { get; set; }
    public Guid AssignmentOperationalId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CollaboratorId { get; set; }
    public string CollaboratorName { get; set; } = default!;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = default!;
    public AssignmentCollaboratorsRoles Role { get; set; }

}