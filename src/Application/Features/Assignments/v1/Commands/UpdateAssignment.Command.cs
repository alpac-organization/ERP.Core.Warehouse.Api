using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class UpdateAssignmentCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid AssignmentId { get; set; }

        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }


        public AssignmentOperationalStatus? Status { get; set; }
        public AssignmentEnclosureData? Enclosure { get; set; }
        public List<AssignmentMachineryData> Machineries { get; set; } = [];
        public List<AssignmentCollaboratorData> Collaborators { get; set; } = [];
    }
}