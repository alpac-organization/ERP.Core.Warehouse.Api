using MediatR;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetLotByIdQuery : BaseRequest, IRequest<LotDto>
{
    public Guid SectionId { get; set; }
    public Guid LotId { get; set; }
}


#region lots por seccion
public class GetLotsBySectionQuery : BaseRequest, IRequest<PagedResponse<LotListItemDto>>
{
    public Guid SectionId { get; set; }
    public string? Code { get; set; }
    public RackStatus? RackStatus { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
#endregion