using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class RackDetailsDto
{
    public Guid RackId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public int RowNumber { get; set; }
    public int LevelNumber { get; set; }
    public int MaxPulleys { get; set; }
    public RackUsageProfile UsageProfile { get; set; }
    public RackStatus Status { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public RackCapacityDto Capacity { get; set; } = new();
    public RackCoordinatesDto Coordinates { get; set; } = new();
    public List<RackPositionDetailDto> Positions { get; set; } = [];
}

public class RackCapacityDto
{
    public Guid RackCapacityId { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal? Height { get; set; }
    public decimal TotalAreaM2 { get; set; }
    public decimal AvailableAreaWithMarginM2 { get; set; }
    public decimal UnusedAreaM2 { get; set; }
    public decimal OccupiedChargeableAreaM2 { get; set; }
    public decimal UnoccupiedChargeableAreaM2 { get; set; }
}

public class RackCoordinatesDto
{
    public Guid RackCoordinateId { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public decimal PositionZ { get; set; }
    public decimal RotationY { get; set; }
}

public class RackPositionDetailDto
{
    public Guid PositionId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public int Level { get; set; }
    public RackStatus Status { get; set; }
    public bool AllowsStocking { get; set; }
    public string? Observations { get; set; }

    public StockPlacementSummaryDto? CurrentStock { get; set; }
}

public class StockPlacementSummaryDto
{
    public Guid StockId { get; set; }
    public string? ProductName { get; set; }
    public string? CategoryName { get; set; }
    public decimal CurrentWeightKg { get; set; }
    public int CurrentBultos { get; set; }
    public DateOnly PlacedAtDate { get; set; }
    public TimeOnly PlacedAtTime { get; set; }
}
