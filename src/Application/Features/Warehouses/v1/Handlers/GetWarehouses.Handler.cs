using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetWarehousesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager,
    IMapper mapper, IWarehouseCapacityCalculator capacityCalculator)
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
            .AsNoTracking()
            .Where(w => w.ParentWarehouseId == null);

        var filteredQuery = ApplyFilters(warehousesQuery, request);

        var totalRecords = await filteredQuery.CountAsync(cancellationToken);

        if (totalRecords == 0)
        {
            return new PagedResponse<WarehouseDto>(
                [],
                request.PageNumber,
                request.PageSize,
                0);
        }

        var pagedWarehouses = await filteredQuery
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .Include(w => w.Details)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = mapper.Map<List<WarehouseDto>>(pagedWarehouses);

        foreach (var dto in mapped)
            await FillCapacityAsync(dto, cancellationToken);

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }

    private async Task FillCapacityAsync(WarehouseDto node, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Capacity is null)
            return;

        var result = await capacityCalculator.CalculateAsync(
            node.WarehouseId,
            node.Capacity.TotalAreaM2,
            node.Capacity.UsableAreaM2,
            cancellationToken);

        node.Capacity.OccupiedAreaM2 = result.OccupiedAreaM2;
        node.Capacity.FreeAreaM2 = result.FreeAreaM2;
        node.Capacity.OccupancyPercentage = result.OccupancyPercentage;
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
}