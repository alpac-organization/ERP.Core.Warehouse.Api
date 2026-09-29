using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetWarehouseCapacitiesQuery : BaseRequest, IRequest<WarehouseCapacitiesDto>
{
    public Guid WarehouseId { get; set; }
}
