using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetWarehouseByIdQuery : BaseRequest, IRequest<WarehouseDetailDto>
{
    public Guid WarehouseId { get; set; }
}