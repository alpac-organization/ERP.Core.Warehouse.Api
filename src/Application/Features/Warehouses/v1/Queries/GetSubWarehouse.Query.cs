using MediatR;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetSubWarehousesQuery : BaseRequest, IPagedQuery, IRequest<PagedResponse<WarehouseDto>>
{
    public Guid ParentWarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsOwner { get; set; }
    public string? Search { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}