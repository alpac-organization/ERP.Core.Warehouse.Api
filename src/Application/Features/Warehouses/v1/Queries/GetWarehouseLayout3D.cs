using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
    public class GetWarehouseLayout3dQuery : BaseRequest, IRequest<WarehouseLayout3dDto>
    {
        public Guid WarehouseId { get; set; }
    }
}