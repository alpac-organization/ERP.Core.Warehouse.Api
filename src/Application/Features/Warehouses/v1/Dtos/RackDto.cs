using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class RackDto
{
    public Guid RackId { get; set; }
    public Guid SectionId { get; set; }
    public string? Code { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal? HeightMetres { get; set; }
    public string? UsageProfile { get; set; }
    public int RowNumber { get; set; }
    public int LevelNumber { get; set; }
    public int MaxPulleys { get; set; }
    public string? Status { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime? StatusChangedAt { get; set; }

    public int TotalPositions { get; set; }
    public int OccupiedPositions { get; set; }
    public List<RackPositionDto> Positions { get; set; } = [];
}

public class RegisterRacksBulkDto
{
    public string? ShelfCode { get; set; } // codigo de estante
    public int? StartingDepositNumber { get; set; }
    public List<RackLevelSpecDto> Levels { get; set; } = [];
}

public class RackLevelSpecDto
{
    public int LevelNumber { get; set; }
    public int RacksCount { get; set; } // cantidad de racks por nivel

    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal? HeightMetres { get; set; } // altura de rack si aplica

    public RackUsageProfile UsageProfile { get; set; }
    public int MaxPulleys { get; set; } = 2;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}

public class RegisterRacksBulkResultDto
{
    public Guid SectionId { get; set; }
    public decimal SectionLengthMetres { get; set; }
    public int TotalRequested { get; set; }
    public int TotalCreated { get; set; }
    public List<LevelCapacityDto> LevelCapacity { get; set; } = [];
    // public List<RackSummaryDto> Racks { get; set; } = [];
}

public class RackSummaryDto
{
    public Guid RackId { get; set; }
    public string Code { get; set; } = null!;
    public int LevelNumber { get; set; }
    public int RowNumber { get; set; }
    public RackStatus Status { get; set; }
}

public class LevelCapacityDto
{
    public int LevelNumber { get; set; }
    public int RacksCount { get; set; }
    public decimal UsedLengthMetres { get; set; }
    public decimal AvailableLengthMetres { get; set; }
}

public class RackSectionFilterResultDto
{
    public Guid SectionId { get; set; }
    public int TotalRacksCount { get; set; }
    public List<RackSummaryDto> Racks { get; set; } = [];
}

public class RackPositionDto
{
    public Guid PositionId { get; set; }
    public int PositionNumber { get; set; }
    public string PositionCode { get; set; } = null!;
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}