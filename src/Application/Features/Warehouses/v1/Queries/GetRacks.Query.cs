using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetRacksBySectionQuery : BaseRequest, IRequest<RackSectionFilterResultDto>
{
    public Guid SectionId { get; set; }

    public int? LevelNumber { get; set; }
    public RackStatus? Status { get; set; }
    public decimal? WidthMetres { get; set; }
    public decimal? LengthMetres { get; set; }
    public decimal? HeightMetres { get; set; }
}

public class GetRackByIdQuery : BaseRequest, IRequest<RackDto>
{
    public Guid RackId { get; set; }
}