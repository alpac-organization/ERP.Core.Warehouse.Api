using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
    public class GetWarehousesQuery : BaseRequest, IRequest<List<WarehouseDto>>
    {
        public string? BranchCode { get; set; }
    }
}