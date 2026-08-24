using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class WarehouseDetailDto : WarehouseDto
{
    public WarehouseDetailsDto Details { get; set; } = new();
    public List<SectionSummaryDto> Sections { get; set; } = new();
    public int TotalRacks { get; set; }
    public int TotalLots { get; set; }
    public int TotalPositions { get; set; }
    public int OccupiedPositions { get; set; }
    public int FreePositions { get; set; }
    public int BlockedPositions { get; set; }
}

public class WarehouseDetailsDto
{
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public int? RampsCount { get; set; }
    public int? ParkingSpacesCount { get; set; }
}

public class SectionSummaryDto
{
    public Guid SectionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SectionType SectionType { get; set; }
    public SectionStorageType StorageType { get; set; }
    public bool IsActive { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal? UsableAreaM2 { get; set; }
    public decimal? OccupiedAreaM2 { get; set; }
    public decimal? FreeAreaM2 { get; set; }
    public decimal? OccupancyPercentage { get; set; }
    public int RacksCount { get; set; }
    public int LotsCount { get; set; }
    public int TotalPositions { get; set; }
    public int OccupiedPositions { get; set; }
    public int FreePositions { get; set; }
    public int BlockedPositions { get; set; }
}