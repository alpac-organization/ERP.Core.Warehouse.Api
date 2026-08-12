using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterRackCommand : BaseRequest, IRequest<RackDto>
{
    public Guid SectionId { get; set; }

    public string Code { get; set; } = null!;
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal HeightMetres { get; set; }

    public RackUsageProfile UsageProfile { get; set; }
    public int RowNumber { get; set; }
    public int LevelNumber { get; set; }
    public int MaxPulleys { get; set; } = 2;

    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}

public class RegisterRacksBulkCommand : BaseRequest, IRequest<RegisterRacksBulkResultDto>
{
    public Guid SectionId { get; set; }
    public string? ShelfCode { get; set; }
    public int? StartingDepositNumber { get; set; }
    public List<RackLevelSpec> Levels { get; set; } = [];
}

public class RackLevelSpec
{
    public int LevelNumber { get; set; }
    public int RacksCount { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal? HeightMetres { get; set; }
    public RackUsageProfile UsageProfile { get; set; }
    public int MaxPulleys { get; set; } = 2;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}