using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class CreateAssignmentCommand : BaseRequest, IRequest<Unit>
    {
        public Guid OperationalOrderId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? Observations { get; set; }
        public string? Merchandise { get; set; }
        public string? MerchandiseDescription { get; set; }
        public DestinationType? DestinationType { get; set; }
    

        public bool HasAssignedMachinery { get; set; } = false;
        public bool HasAssignedCollaborators { get; set; } = false;

        public List<AssignedMachinery> AssignedMachineries { get; set; } = [];
        public List<AssignedCollaborator> AssignedCollaborators { get; set; } = [];
    }

    public class AssignedMachinery
    {
        public string? Concept { get; set; }
        public Guid MachineryId { get; set; }
    }

    public class AssignedCollaborator
    {
        public Guid CollaboratorId { get; set; }
        public AssignmentCollaboratorsRoles Role { get; set; }
    }
}