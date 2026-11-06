using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;

public class GetFuelRequestsDetailQuery : BaseRequest, IRequest<GetFuelRequestDetailDto>
{
    public Guid FuelRequestId { get; set; }
}