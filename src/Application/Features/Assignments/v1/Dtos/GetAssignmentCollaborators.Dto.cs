using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

public class GetAssignmentCollaboratorsDto
{
    public Guid AssignmentOperationalId { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedByUserName { get; set; } = default!;
    public Guid AssignmentCollaboratorId { get; set; }
    public Guid CollaboratorId { get; set; }
    public AssignmentCollaboratorsRoles Role { get; set; }
    public CollaboratorInformation CollaboratorInformation { get; set; } = default!;
}

public class CollaboratorInformation
{
    public string CollaboratorName { get; set; } = default!;

    public string WorkAreaName { get; set; } = default!;
    public string JobPositionName { get; set; } = default!;
}