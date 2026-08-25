using AutoMapper;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Utils;
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

        var dto = new WarehouseDetailDto
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

        if (warehouse.Details != null)
        {
            dto.Details = new WarehouseDetailsDto();

            if (warehouse.Details.RampsCount.HasValue && warehouse.Details.RampsCount > 0)
                dto.Details.RampsCount = warehouse.Details.RampsCount;
            if (warehouse.Details.ParkingSpacesCount.HasValue && warehouse.Details.ParkingSpacesCount > 0)
                dto.Details.ParkingSpacesCount = warehouse.Details.ParkingSpacesCount;
            dto.Details.WidthMetres = warehouse.Details.WitdhMetres;
            dto.Details.LengthMetres = warehouse.Details.LengthMetres;

        }

        if (dto.Capacity != null)
        {
            var result = capacityCalculator.Calculate(warehouse);

            dto.Capacity.TotalAreaM2 = result.TotalAreaM2;
            dto.Capacity.UsableAreaM2 = result.UsableAreaM2;
            dto.Capacity.UnusableAreaM2 = result.UnusableAreaM2;
            dto.Capacity.OccupiedAreaM2 = result.OccupiedAreaM2;
            dto.Capacity.FreeAreaM2 = result.FreeAreaM2;
            dto.Capacity.OccupancyPercentage = result.OccupancyPercentage;
            dto.Capacity.TotalPositions = result.TotalPositions;
            dto.Capacity.UsedPositions = result.UsedPositions;
            dto.Capacity.FreePositions = result.FreePositions;
        }

        var sections = warehouse.Sections ?? new List<Sections>();
        var sectionSummaries = new List<SectionSummaryDto>();
        int totalRacks = 0, totalLots = 0;
        int totalPositions = 0, occupiedPositions = 0, freePositions = 0, blockedPositions = 0;

        foreach (var section in sections)
        {
            var summary = new SectionSummaryDto
            {
                SectionId = section.Id,
                Code = section.Code,
                Name = section.Name,
                SectionType = section.SectionType,
                StorageType = section.StorageType,
                IsActive = section.IsActive,
                WidthMetres = section.WidthMetres,
                LengthMetres = section.LengthMetres
            };

            if (section.Capacity != null)
            {
                summary.UsableAreaM2 = section.Capacity.UsableAreaM2;
            }

            var racks = section.Racks ?? new List<Racks>();
            summary.RacksCount = racks.Count;
            totalRacks += racks.Count;

            var lots = section.Lots ?? new List<Lots>();
            summary.LotsCount = lots.Count;
            totalLots += lots.Count;

            int sectionRackPositions = racks.Sum(r => PositionMetrics.Total(r.Positions));
            int sectionRackOccupied = racks.Sum(r =>
                PositionMetrics.Occupied(r.Positions, p => p.IsOccupied));
            int sectionRackBlocked = racks.Sum(r =>
                PositionMetrics.Blocked(r.Positions, p => p.IsBlocked && !p.IsOccupied));

            int sectionLotPositions = lots.Sum(l => PositionMetrics.Total(l.Positions));
            int sectionLotOccupied = lots.Sum(l =>
                PositionMetrics.Occupied(l.Positions, p => p.IsOccupied));
            int sectionLotBlocked = lots.Sum(l =>
                PositionMetrics.Blocked(l.Positions, p => p.IsBlocked && !p.IsOccupied));

            summary.TotalPositions = sectionRackPositions + sectionLotPositions;
            summary.OccupiedPositions = sectionRackOccupied + sectionLotOccupied;
            summary.BlockedPositions = sectionRackBlocked + sectionLotBlocked;
            summary.FreePositions = summary.TotalPositions -
                summary.OccupiedPositions - summary.BlockedPositions;

            sectionSummaries.Add(summary);

            // Acumular totales generales
            totalPositions += summary.TotalPositions;
            occupiedPositions += summary.OccupiedPositions;
            freePositions += summary.FreePositions;
            blockedPositions += summary.BlockedPositions;
        }

        dto.Sections = sectionSummaries;
        dto.TotalRacks = totalRacks;
        dto.TotalLots = totalLots;
        dto.TotalPositions = totalPositions;
        dto.OccupiedPositions = occupiedPositions;
        dto.FreePositions = freePositions;
        dto.BlockedPositions = blockedPositions;

        return Task.FromResult(dto);
    }
}