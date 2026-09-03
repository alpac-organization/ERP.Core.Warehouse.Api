using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class RackPositionDto
{
    public Guid PositionId { get; set; }
    public int PositionNumber { get; set; }
    public string PositionCode { get; set; } = null!;
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool IsOccupied { get; set; }
}

public class RackListDto
{
    public Guid RackId { get; set; }
    public string Code { get; set; } = null!;

    public Guid SectionId { get; set; }
    public int LevelNumber { get; set; }
    public int RowNumber { get; set; }
    public RackStatus Status { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal? HeightMetres { get; set; }

    public RackUsageProfile UsageProfile { get; set; }

    public LayoutTransform3DDto? Transform { get; set; }

    public int TotalPositions { get; set; }
    public int OccupiedPositions { get; set; }
    public List<RackPositionDto> Positions { get; set; } = [];
}