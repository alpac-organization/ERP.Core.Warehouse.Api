using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetWarehousesQuery : BaseRequest, IRequest<PagedResponse<WarehouseDto>>
{

    //IdLocation
    // public string? BranchCode { get; set; }
    public string? WarehouseCode { get; set; }
    public WarehouseType? WarehouseType { get; set; }

    public bool? IsActive { get; set; }

    //Eliminar
    public bool? IsOwner { get; set; }
    
    //Eliminar
    public string? Search { get; set; }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}