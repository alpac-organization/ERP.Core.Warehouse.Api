using MediatR;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

public class CreateAssignmentCollaboratorsCommand : BaseRequest, IRequest<Unit>
{
    public Guid OperationalOrderId { get; set; }
    public Guid AssignmentOperationalId { get; set; }
    public List<Guid> Collaborators { get; set; } = [];
    public AssignmentCollaboratorsRoles Role { get; set; } = AssignmentCollaboratorsRoles.WarehouseAssistant;
}
