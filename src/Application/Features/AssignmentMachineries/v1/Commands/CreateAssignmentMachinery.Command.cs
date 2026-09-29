using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.AssignmentMachineries.v1.Commands;

public class CreateAssignmentMachineryCommand : BaseRequest, IRequest<Unit>
{
    public Guid OperationalOrderId { get; set; }
    public Guid MachineryId { get; set; }
}