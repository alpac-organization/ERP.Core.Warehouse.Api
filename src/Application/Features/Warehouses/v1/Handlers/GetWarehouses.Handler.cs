using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;

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

        var warehousesQuery = _unitOfWork.Warehouses.Entities.AsNoTracking();

        var filteredQuery = ApplyFilters(warehousesQuery, request);

        var allMatchingWarehouses = await filteredQuery
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .ToListAsync(cancellationToken);

        var matchingIds = allMatchingWarehouses.Select(w => w.Id).ToHashSet();

        if (matchingIds.Count == 0)
        {
            return new PagedResponse<WarehouseDto>(
                [],
                request.PageNumber,
                request.PageSize,
                0);
        }

        // Obtener TODAS las bodegas necesarias para construir el árbol completo
        var allNeededIds = new HashSet<Guid>(matchingIds);

        foreach (var id in matchingIds)
        {
            var ancestors = await GetAncestorsAsync(id, cancellationToken);
            foreach (var ancestorId in ancestors)
                allNeededIds.Add(ancestorId);

            var descendants = await GetAllDescendantsAsync(id, cancellationToken);
            foreach (var descendantId in descendants)
                allNeededIds.Add(descendantId);
        }

        var allWarehouses = await _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(w => allNeededIds.Contains(w.Id))
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .ToListAsync(cancellationToken);

        // Construir el árbol completo
        var warehouseDict = allWarehouses.ToDictionary(w => w.Id);
        var roots = new List<WarehouseEntity>();

        foreach (var w in allWarehouses)
            w.SubWarehouses = [];

        foreach (var warehouse in allWarehouses)
        {
            if (warehouse.ParentWarehouseId == null)
            {
                roots.Add(warehouse);
            }
            else if (warehouseDict.TryGetValue(warehouse.ParentWarehouseId.Value, out var parent))
            {
                if (!parent.SubWarehouses.Contains(warehouse))
                    parent.SubWarehouses.Add(warehouse);
            }
        }

        // APLANAR EL ÁRBOL: Obtener todos los nodos en orden
        var allNodesInOrder = new List<WarehouseEntity>();
        FlattenTree(roots, allNodesInOrder);

        // APLICAR PAGINACIÓN SOBRE LOS NODOS APLANADOS
        var pagedNodes = allNodesInOrder
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Obtener IDs de los nodos paginados
        var pagedNodeIds = pagedNodes.Select(n => n.Id).ToHashSet();

        // Filtrar el árbol para mantener solo los nodos paginados y sus ancestros
        var filteredRoots = roots
            .Select(root => FilterTreeForPagination(root, pagedNodeIds))
            .Where(root => root != null)
            .Select(root => root!)
            .ToList();

        var totalRecords = allNodesInOrder.Count;

        var mapped = filteredRoots
            .Select(root => MapWithChildren(root))
            .ToList();

        foreach (var root in mapped)
            await FillCapacityAsync(root, cancellationToken);

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }

    // Nuevo método para aplanar el árbol
    private void FlattenTree(List<WarehouseEntity> nodes, List<WarehouseEntity> result)
    {
        foreach (var node in nodes.OrderBy(n => n.Code))
        {
            result.Add(node);
            if (node.SubWarehouses != null && node.SubWarehouses.Any())
            {
                FlattenTree(node.SubWarehouses.ToList(), result);
            }
        }
    }

    private async Task<List<Guid>> GetAncestorsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var ancestors = new List<Guid>();
        var currentId = warehouseId;

        while (true)
        {
            var warehouse = await _unitOfWork.Warehouses.Entities
                .AsNoTracking()
                .Where(w => w.Id == currentId)
                .Select(w => new { w.Id, w.ParentWarehouseId })
                .FirstOrDefaultAsync(cancellationToken);

            if (warehouse == null || warehouse.ParentWarehouseId == null)
                break;

            ancestors.Add(warehouse.ParentWarehouseId.Value);
            currentId = warehouse.ParentWarehouseId.Value;
        }

        return ancestors;
    }

    private async Task<List<Guid>> GetAllDescendantsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var descendants = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(warehouseId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var children = await _unitOfWork.Warehouses.Entities
                .AsNoTracking()
                .Where(w => w.ParentWarehouseId == currentId)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);

            foreach (var childId in children)
            {
                descendants.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return descendants;
    }

    private WarehouseDto MapWithChildren(WarehouseEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var dto = mapper.Map<WarehouseDto>(entity);

        if (entity.SubWarehouses != null && entity.SubWarehouses.Any())
        {
            dto.SubWarehouses = entity.SubWarehouses
                .OrderBy(c => c.Code)
                .Select(c => MapWithChildren(c))
                .ToList();
        }
        else
        {
            dto.SubWarehouses = [];
        }

        return dto;
    }

    private async Task FillCapacityAsync(WarehouseDto node, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);

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
        if (request.IsActive.HasValue)
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
                ware.Code.Contains(request.Search) ||
                ware.WarehouseName.Contains(request.Search));

        return query;
    }

    private WarehouseEntity? FilterTreeForPagination(
        WarehouseEntity node,
        HashSet<Guid> pagedNodeIds)
    {
        if (pagedNodeIds.Contains(node.Id))
            return node;

        var filteredChildren = node.SubWarehouses?
            .Select(child => FilterTreeForPagination(child, pagedNodeIds))
            .Where(child => child != null)
            .Select(child => child!)
            .ToList() ?? [];

        if (filteredChildren.Count > 0)
        {
            return new WarehouseEntity
            {
                Id = node.Id,
                Code = node.Code,
                WarehouseName = node.WarehouseName,
                IsActive = node.IsActive,
                IsOwner = node.IsOwner,
                WarehouseType = node.WarehouseType,
                ParentWarehouseId = node.ParentWarehouseId,
                BranchId = node.BranchId,
                Branch = node.Branch ?? new Branch(),
                Capacity = node.Capacity,
                SubWarehouses = filteredChildren,
                Details = node.Details ?? new WarehouseDetails(),
                Sections = node.Sections ?? []
            };
        }

        return null;
    }
}