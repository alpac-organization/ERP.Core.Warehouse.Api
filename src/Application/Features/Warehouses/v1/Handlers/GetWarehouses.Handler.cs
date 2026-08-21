using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
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

        // 1. Query base: solo bodegas raíz (sin padre)
        var warehousesQuery = _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(w => w.ParentWarehouseId == null);

        // 2. Aplicar filtros
        var filteredQuery = ApplyFilters(warehousesQuery, request);

        // 3. Total de registros (para paginación)
        var totalRecords = await filteredQuery.CountAsync(cancellationToken);

        if (totalRecords == 0)
        {
            return new PagedResponse<WarehouseDto>(
                [],
                request.PageNumber,
                request.PageSize,
                0);
        }

        // 4. Obtener la página solicitada (incluyendo datos necesarios)
        var pagedWarehouses = await filteredQuery
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .Include(w => w.Details)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 5. Mapear a DTOs (HasChildren se mapea directamente desde la entidad)
        var mapped = mapper.Map<List<WarehouseDto>>(pagedWarehouses);

        // 6. Calcular capacidades (opcional)
        foreach (var dto in mapped)
            await FillCapacityAsync(dto, cancellationToken);

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            totalRecords);
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
}