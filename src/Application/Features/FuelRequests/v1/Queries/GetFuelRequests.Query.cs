using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;

public class GetFuelRequestsQuery : BaseSupplyRequestQuery, IRequest<PagedResponse<GetFuelRequestDto>>
{
    public Guid? MachineryId { get; set; }
    public Guid? ProductStockId { get; set; }
}