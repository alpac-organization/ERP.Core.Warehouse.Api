
namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

using ERP.Core.Database.Domain.Enums;


public class LotDto
{
    public Guid LotId { get; set; }
    public Guid SectionId { get; set; }
    public string? Code { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public int NominalRows { get; set; }
    public int NominalColumns { get; set; }
    public bool AllowsStacking { get; set; }
    public RackStatus? Status { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime? StatusChangedAt { get; set; }

    public LayoutTransform3DDto? Transform { get; set; }

    public int TotalPositions { get; set; }
    public int OccupiedPositions { get; set; }
    public int BlockedPositions { get; set; }
    public int FreePositions { get; set; }

    public List<LotPositionDto> Positions { get; set; } = [];
}

public class LotPositionDto
{
    public Guid PositionId { get; set; }
    public int RowNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string PositionCode { get; set; } = null!;
    public bool AllowsStacking { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsOccupied { get; set; }
    public string? BlockReason { get; set; }
}

public class LotListItemDto
{
    public Guid LotId { get; set; }
    public string? Code { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public RackStatus? Status { get; set; }

    public int TotalPositions { get; set; }
    public int UsedPositions { get; set; }

    public LayoutTransform3DDto? Transform { get; set; }

    public decimal TotalAreaM2 { get; set; }
    public decimal UsedAreaM2 { get; set; }
    public decimal? OccupancyPercentage { get; set; }
}