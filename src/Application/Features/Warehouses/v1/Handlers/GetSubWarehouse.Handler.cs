using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSubWarehousesHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IWarehouseCapacityCalculator capacityCalculator)
    : BaseValidatorHandler<GetSubWarehousesQuery, PagedResponse<WarehouseDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<WarehouseDto>> Handle(
        GetSubWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var query = _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(w => w.ParentWarehouseId == request.ParentWarehouseId);

        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        if (request.IsOwner.HasValue)
            query = query.Where(w => w.IsOwner == request.IsOwner.Value);

        if (!string.IsNullOrWhiteSpace(request.WarehouseCode))
            query = query.Where(w => w.Code == request.WarehouseCode);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(w =>
                w.Code.Contains(request.Search) ||
                w.WarehouseName.Contains(request.Search));

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
            .OrderBy(w => w.Code)
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
}