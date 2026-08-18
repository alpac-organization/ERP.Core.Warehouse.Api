using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
    public class GetWarehousesQuery : BaseRequest, IRequest<PagedResponse<WarehouseDto>>
    {
        public string? BranchCode { get; set; }
        public string? WarehouseCode { get; set; }
        public WarehouseType? WarehouseType { get; set; }

        public bool? IsActive { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}