using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class CreateAssignmentMachineryCommand : BaseRequest, IRequest<Unit>, IAssignmentOperationalRequest
{
    [JsonIgnore]
    public Guid OperationalOrderId { get; set; }

    public string? Concept { get; set; }

    [JsonIgnore]
    public Guid AssignmentOperationalId { get; set; }

    public List<Guid> Machinery { get; set; } = [];
}
