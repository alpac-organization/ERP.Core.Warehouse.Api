using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

public class DeclineSupplyRequestCommand : BaseRequest, IRequest<Unit>
{
    public Guid SupplyRequestId { get; set; }
    public string? Observations { get; set; }
}