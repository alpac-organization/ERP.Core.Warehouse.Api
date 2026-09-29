using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class CreateAssignmentCommand : BaseRequest, IRequest<Unit>
    {
        public Guid OperationalOrderId { get; set; }
        public AssignmentOperationalStatus Status { get; set; } = AssignmentOperationalStatus.Pending;

        public AssignmentEnclosureData? Enclosure { get; set; }
        public List<AssignmentMachineryData> Machineries { get; set; } = [];
        public List<AssignmentCollaboratorData> Collaborators { get; set; } = [];
    }
}