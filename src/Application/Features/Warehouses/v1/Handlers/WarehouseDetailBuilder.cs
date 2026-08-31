using AutoMapper;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public static class WarehouseDetailBuilder
{
    public static Task<WarehouseDetailDto> BuildDetailAsync(
        WarehouseEntity warehouse,
        IMapper mapper,
        IWarehouseCapacityCalculator capacityCalculator,
        CancellationToken cancellationToken)
    {
        var baseDto = mapper.Map<WarehouseDto>(warehouse);

        var dto = CreateDetailDto(baseDto);
        ApplyWarehouseDetails(dto, warehouse);

        if (dto.Capacity != null)
            WarehouseCapacityMapper.Apply(dto, capacityCalculator.Calculate(warehouse));

        ApplySectionData(dto, warehouse.Sections);

        return Task.FromResult(dto);
    }

    private static WarehouseDetailDto CreateDetailDto(WarehouseDto baseDto) => new()
    {
        WarehouseId = baseDto.WarehouseId,
        WarehouseName = baseDto.WarehouseName,
        WarehouseCode = baseDto.WarehouseCode,
        IsActive = baseDto.IsActive,
        WarehouseType = baseDto.WarehouseType,
        IsOwner = baseDto.IsOwner,
        BranchCode = baseDto.BranchCode,
        SectionsCount = baseDto.SectionsCount,
        Capacity = baseDto.Capacity,
        HasChildren = baseDto.HasChildren
    };

    private static void ApplyWarehouseDetails(WarehouseDetailDto dto, WarehouseEntity warehouse)
    {
        if (warehouse.Details is null)
            return;

        dto.Details = new WarehouseDetailsDto
        {
            RampsCount = warehouse.Details.RampsCount > 0 ? warehouse.Details.RampsCount : null,
            ParkingSpacesCount = warehouse.Details.ParkingSpacesCount > 0
                ? warehouse.Details.ParkingSpacesCount
                : null,
            WidthMetres = warehouse.Details.WitdhMetres,
            LengthMetres = warehouse.Details.LengthMetres
        };
    }

    private static void ApplySectionData(WarehouseDetailDto dto, IEnumerable<Sections>? sections)
    {
        var summaries = (sections ?? []).Select(BuildSectionSummary).ToList();

        dto.Sections = summaries;
        dto.TotalRacks = summaries.Sum(summary => summary.RacksCount);
        dto.TotalLots = summaries.Sum(summary => summary.LotsCount);
        dto.TotalPositions = summaries.Sum(summary => summary.TotalPositions);
        dto.OccupiedPositions = summaries.Sum(summary => summary.OccupiedPositions);
        dto.FreePositions = summaries.Sum(summary => summary.FreePositions);
        dto.BlockedPositions = summaries.Sum(summary => summary.BlockedPositions);
    }

    private static SectionSummaryDto BuildSectionSummary(Sections section)
    {
        var isLotsSection = section.StorageType == ERP.Core.Database.Domain.Enums.SectionStorageType.Lots;
        var isRacksSection = section.StorageType == ERP.Core.Database.Domain.Enums.SectionStorageType.Racks;
        var racks = section.Racks ?? [];
        var lots = section.Lots ?? [];
        var metrics = isLotsSection
            ? PositionMetrics.Summarize<Lots, LotsPositions>(
                lots, lot => lot.Positions, position => position.IsOccupied || position.IsBlocked || position.IsReserved,
                lot => lot.WidthMetres, lot => lot.LengthMetres)
            : isRacksSection
            ? PositionMetrics.Summarize<Racks, RackPositions>(
                racks, rack => rack.Positions, position => position.IsOccupied || position.IsBlocked || position.IsReserved,
                rack => rack.WidthMetres, rack => rack.LengthMetres)
            : new PositionMetrics.Summary(0, 0, 0, 0);

        var summary = new SectionSummaryDto
        {
            SectionId = section.Id,
            Code = section.Code,
            Name = section.Name,
            SectionType = section.SectionType,
            StorageType = section.StorageType,
            IsActive = section.IsActive,
            WidthMetres = section.WidthMetres,
            LengthMetres = section.LengthMetres,
            UsableAreaM2 = metrics.TotalAreaM2,
            OccupiedAreaM2 = metrics.UsedAreaM2,
            FreeAreaM2 = Math.Max(0, metrics.TotalAreaM2 - metrics.UsedAreaM2),
            OccupancyPercentage = metrics.TotalAreaM2 > 0
                ? Math.Round(metrics.UsedAreaM2 / metrics.TotalAreaM2 * 100, 2)
                : 0,
            RacksCount = isRacksSection ? racks.Count : 0,
            LotsCount = isLotsSection ? lots.Count : 0,
            TotalPositions = metrics.TotalPositions
        };

        SetPositionCounts(summary, section, isLotsSection);
        return summary;
    }

    private static void SetPositionCounts(
        SectionSummaryDto summary,
        Sections section,
        bool isLotsSection)
    {
        if (isLotsSection)
        {
            var positions = (section.Lots ?? []).SelectMany(lot => lot.Positions ?? []);
            summary.OccupiedPositions = PositionMetrics.Occupied(positions, position => position.IsOccupied);
            summary.BlockedPositions = PositionMetrics.Blocked(
                positions, position => position.IsBlocked && !position.IsOccupied);
        }
        else if (section.StorageType == ERP.Core.Database.Domain.Enums.SectionStorageType.Racks)
        {
            var positions = (section.Racks ?? []).SelectMany(rack => rack.Positions ?? []);
            summary.OccupiedPositions = PositionMetrics.Occupied(positions, position => position.IsOccupied);
            summary.BlockedPositions = PositionMetrics.Blocked(
                positions, position => position.IsBlocked && !position.IsOccupied);
        }

        summary.FreePositions = PositionMetrics.Free(
            summary.TotalPositions,
            summary.OccupiedPositions,
            summary.BlockedPositions);
    }
}