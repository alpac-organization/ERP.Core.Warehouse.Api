using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class AssignmentCollaboratorData
    {
        public Guid CollaboratorId { get; set; }
        public AssignmentCollaboratorsRoles Role { get; set; }
    }
}