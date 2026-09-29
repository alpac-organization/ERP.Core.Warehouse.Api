using MediatR;

using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class CreateAssignmentMachineryCommand : BaseRequest, IRequest<Unit>
{
    public Guid OperationalOrderId { get; set; }
    public string? Concept { get; set; }
    public Guid AssignmentOperationalId { get; set; }
    public List<Guid> Machinery { get; set; } = [];
}
