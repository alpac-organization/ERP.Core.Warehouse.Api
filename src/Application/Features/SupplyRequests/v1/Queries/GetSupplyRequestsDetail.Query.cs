using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;

public class GetSupplyRequestsDetailQuery : BaseRequest, IRequest<GetSupplyRequestDetailDto>
{
    public Guid SupplyRequestId { get; set; }
}