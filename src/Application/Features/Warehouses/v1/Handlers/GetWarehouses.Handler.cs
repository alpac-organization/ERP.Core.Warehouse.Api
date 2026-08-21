using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetWarehousesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetWarehousesQuery, PagedResponse<WarehouseDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<WarehouseDto>> Handle(
        GetWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var warehousesQuery = _unitOfWork.Warehouses.Entities
            .AsNoTracking();

        warehousesQuery = ApplyFilters(warehousesQuery, request);

        var totalRecords = await warehousesQuery.CountAsync(cancellationToken);

        var roots = await warehousesQuery
            .OrderBy(ware => ware.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(ware => ware.Capacity)
            .ToListAsync(cancellationToken);

        // Carga el árbol completo (todos los niveles) para esta página de raíces
        var descendantsByParent = await LoadDescendantsGroupedByParentAsync(
            roots.Select(r => r.Id).ToList(), cancellationToken);

        // Pasada 1 (síncrona): arma el árbol de DTOs con Level, sin capacidad todavía
        var mapped = roots
            .Select(root => MapWithChildren(root, descendantsByParent, level: 0))
            .ToList();

        // Pasada 2 (asíncrona): recorre el árbol ya armado y llena Capacity
        foreach (var root in mapped)
            await FillCapacityAsync(root, cancellationToken);

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }

    private async Task<Dictionary<Guid, List<WarehouseEntity>>> LoadDescendantsGroupedByParentAsync(
        List<Guid> rootIds,
        CancellationToken cancellationToken)
    {
        var byParent = new Dictionary<Guid, List<WarehouseEntity>>();
        var currentLevelParentIds = rootIds;

        while (currentLevelParentIds.Count > 0)
        {
            var children = await _unitOfWork.Warehouses.Entities
                .AsNoTracking()
                .Where(w => w.ParentWarehouseId != null
                    && currentLevelParentIds.Contains(w.ParentWarehouseId.Value)
                    && w.IsActive)
                .Include(w => w.Capacity)
                .ToListAsync(cancellationToken);

            if (children.Count == 0)
                break;

            foreach (var group in children.GroupBy(c => c.ParentWarehouseId!.Value))
                byParent[group.Key] = group.ToList();

            currentLevelParentIds = children.Select(c => c.Id).ToList();
        }

        return byParent;
    }

    private WarehouseDto MapWithChildren(
        WarehouseEntity entity,
        Dictionary<Guid, List<WarehouseEntity>> descendantsByParent,
        int level)
    {
        var dto = mapper.Map<WarehouseDto>(entity);
        dto.Level = level;

        dto.SubWarehouses = descendantsByParent.TryGetValue(entity.Id, out var children)
            ? children
                .OrderBy(c => c.Code)
                .Select(c => MapWithChildren(c, descendantsByParent, level + 1))
                .ToList()
            : [];

        return dto;
    }

    private async Task FillCapacityAsync(WarehouseDto node, CancellationToken cancellationToken)
    {
        var occupiedAreaM2 = await CalculateOccupiedAreaAsync(node.WarehouseId, cancellationToken);

        if (node.Capacity is not null)
        {
            var usableArea = node.Capacity.UsableAreaM2 ?? node.Capacity.TotalAreaM2;
            node.Capacity.OccupiedAreaM2 = occupiedAreaM2;
            node.Capacity.FreeAreaM2 = usableArea - occupiedAreaM2;
            node.Capacity.OccupancyPercentage = usableArea > 0
                ? Math.Round(occupiedAreaM2 / usableArea * 100, 2)
                : 0;
        }

        foreach (var child in node.SubWarehouses)
            await FillCapacityAsync(child, cancellationToken);
    }

    private async Task<decimal> CalculateOccupiedAreaAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var rackAreas = await _unitOfWork.Racks.Entities
            .Where(r => r.Section.WarehouseId == warehouseId)
            .Select(r => new
            {
                AreaTotal = r.WidthMetres * r.LengthMetres,
                PositionsCount = r.Positions.Count,
                OccupiedCount = r.Positions.Count(p => p.CurrentStock.Any())
            })
            .ToListAsync(cancellationToken);

        var lotAreas = await _unitOfWork.Lots.Entities
            .Where(l => l.Section.WarehouseId == warehouseId)
            .Select(l => new
            {
                AreaTotal = l.WidthMetres * l.LengthMetres,
                PositionsCount = l.Positions.Count,
                OccupiedCount = l.Positions.Count(p => p.CurrentStock != null)
            })
            .ToListAsync(cancellationToken);

        var occupied = rackAreas
            .Where(r => r.PositionsCount > 0)
            .Sum(r => (r.AreaTotal / r.PositionsCount) * r.OccupiedCount);

        occupied += lotAreas
            .Where(l => l.PositionsCount > 0)
            .Sum(l => (l.AreaTotal / l.PositionsCount) * l.OccupiedCount);

        return occupied;
    }

    private static IQueryable<WarehouseEntity> ApplyFilters(
        IQueryable<WarehouseEntity> query,
        GetWarehousesQuery request)
    {
        if ( request.IsActive.HasValue)
            query = query.Where(ware => ware.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.BranchCode))
            query = query.Where(ware => ware.Branch.BranchCode == request.BranchCode);

        if (!string.IsNullOrWhiteSpace(request.WarehouseCode))
            query = query.Where(ware => ware.Code == request.WarehouseCode);

        if (request.WarehouseType.HasValue)
            query = query.Where(ware => ware.WarehouseType == request.WarehouseType.Value);

        if (request.IsOwner.HasValue)
            query = query.Where(ware => ware.IsOwner == request.IsOwner.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(ware =>
                ware.Code.Contains(request.Search) || ware.WarehouseName.Contains(request.Search));

        return query;
    }
}