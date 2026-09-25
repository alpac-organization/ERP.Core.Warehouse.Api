using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

public class CreateServiceOrderCommand : BaseRequest, IRequest<Unit>
{
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Observations { get; set; }
    public bool IsCreatedFromPortal { get; set; } = false;
}