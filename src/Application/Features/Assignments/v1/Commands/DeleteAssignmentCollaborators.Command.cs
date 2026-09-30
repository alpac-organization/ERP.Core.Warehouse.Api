using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class DeleteAssignmentCollaboratorsCommand : BaseRequest, IRequest<bool>, IAssignmentOperationalRequest
{
    [JsonIgnore]
    public Guid OperationalOrderId { get; set; }

    [JsonIgnore]
    public Guid AssignmentId { get; set; }

    [JsonIgnore]
    public Guid AssignmentCollaboratorId { get; set; }
}
