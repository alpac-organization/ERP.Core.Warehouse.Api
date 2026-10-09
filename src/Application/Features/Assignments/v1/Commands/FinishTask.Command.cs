using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class FinishTaskCommand : BaseRequest, IRequest<Unit>
{
    [JsonIgnore]
    public Guid OperationalOrderId { get; set; }

    [JsonIgnore]
    public Guid AssignmentId { get; set; }
}