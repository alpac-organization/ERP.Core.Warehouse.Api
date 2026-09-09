using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetLotsBySectionQuery : BaseRequest, IRequest<PagedResponse<LotListItemDto>>
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
    public string? Code { get; set; }
    public RackStatus? RackStatus { get; set; }
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
}