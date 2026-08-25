using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

internal static class WarehousePagedQuery
{
    public static async Task<PagedResponse<WarehouseDto>> ExecuteAsync(
        IQueryable<WarehouseEntity> query,
        IPagedQuery request,
        IMapper mapper,
        IWarehouseCapacityCalculator capacityCalculator,
        CancellationToken cancellationToken)
    {
        var totalRecords = await query.CountAsync(cancellationToken);

        if (totalRecords == 0)
        {
            return new PagedResponse<WarehouseDto>(
                [],
                request.PageNumber,
                request.PageSize,
                0);
        }

        var pagedWarehouses = await query
            .AsSplitQuery()
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .Include(w => w.Details)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Racks)
                    .ThenInclude(r => r.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Lots)
                    .ThenInclude(l => l.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Capacity)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = mapper.Map<List<WarehouseDto>>(pagedWarehouses);

        foreach (var (warehouse, dto) in pagedWarehouses.Zip(mapped))
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Capacity is null)
                continue;

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

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }
}