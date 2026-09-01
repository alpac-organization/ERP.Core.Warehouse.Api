using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

public class GetAssignmentQueueQuery : BaseRequest, IRequest<GetAssignmentQueueDto>, IPagedQuery
{
    public string? ServiceOrderCode { get; set; }
    public string? DucatNumber { get; set; }
    public string? WarehouseName { get; set; }
    public UnloadingStatus? UnloadingStatus { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}