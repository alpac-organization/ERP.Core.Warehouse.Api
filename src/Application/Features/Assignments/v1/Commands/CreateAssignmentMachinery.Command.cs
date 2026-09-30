using MediatR;

using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class CreateAssignmentMachineryCommand : BaseRequest, IRequest<Unit>, IAssignmentOperationalRequest
{
    public Guid OperationalOrderId { get; set; }
    public string? Concept { get; set; }
    public Guid AssignmentOperationalId { get; set; }
    public List<Guid> Machinery { get; set; } = [];
}
