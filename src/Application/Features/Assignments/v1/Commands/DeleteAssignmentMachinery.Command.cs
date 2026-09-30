using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class DeleteAssignmentMachineryCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid OperationalOrderId { get; set; }

    [JsonIgnore]
    public Guid AssignmentId { get; set; }

    [JsonIgnore]
    public Guid AssignmentMachineryId { get; set; }
}
