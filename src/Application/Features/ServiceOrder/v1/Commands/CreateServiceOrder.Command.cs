using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

public class CreateServiceOrderCommand : BaseRequest, IRequest<CreateServiceOrderResponse>
{
    public Guid? CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public string? Observations { get; set; }
    public bool IsCreatedFromPortal { get; set; } = false;
}