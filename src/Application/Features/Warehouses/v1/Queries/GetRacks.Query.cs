using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetRacksBySectionQuery : BaseRequest, IPagedQuery, IRequest<PagedResponse<RackListDto>>
{
    public Guid SectionId { get; set; }

    public int? LevelNumber { get; set; }
    public RackStatus? Status { get; set; }
    public RackUsageProfile? UsageProfile { get; set; }
    public decimal? WidthMetres { get; set; }
    public decimal? LengthMetres { get; set; }
    public decimal? HeightMetres { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}