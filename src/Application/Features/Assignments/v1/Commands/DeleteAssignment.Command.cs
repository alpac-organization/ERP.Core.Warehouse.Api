using MediatR;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class DeleteAssignmentCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid AssignmentId { get; set; }

        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }
    }
}