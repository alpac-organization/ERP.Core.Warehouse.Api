
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
    public class GetWarehouseLayout3dQuery : BaseRequest, IRequest<WarehouseLayout3dDto>
    {
        public Guid WarehouseId { get; set; }
    }
}