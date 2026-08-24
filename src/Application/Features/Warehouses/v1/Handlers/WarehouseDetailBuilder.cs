using AutoMapper;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public static class WarehouseDetailBuilder
{
    public static async Task<WarehouseDetailDto> BuildDetailAsync(
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
            var result = await capacityCalculator.CalculateAsync(
                dto.WarehouseId,
                dto.Capacity.TotalAreaM2,
                dto.Capacity.UsableAreaM2,
                cancellationToken);

            dto.Capacity.OccupiedAreaM2 = result.OccupiedAreaM2;
            dto.Capacity.FreeAreaM2 = result.FreeAreaM2;
            dto.Capacity.OccupancyPercentage = result.OccupancyPercentage;
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

            int sectionRackPositions = racks.Sum(r => r.Positions?.Count ?? 0);
            int sectionRackOccupied = racks.Sum(r => r.Positions?.Count(p => p.IsOccupied) ?? 0);
            int sectionRackBlocked = racks.Sum(r => r.Positions?.Count(p => p.IsBlocked && !p.IsOccupied) ?? 0);

            int sectionLotPositions = lots.Sum(l => l.Positions?.Count ?? 0);
            int sectionLotOccupied = lots.Sum(l => l.Positions?.Count(p => p.IsOccupied) ?? 0);
            int sectionLotBlocked = lots.Sum(l => l.Positions?.Count(p => p.IsBlocked && !p.IsOccupied) ?? 0);

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

        return dto;
    }
}