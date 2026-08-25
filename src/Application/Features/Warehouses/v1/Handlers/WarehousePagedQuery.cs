using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Warehouse.Api.Application.Commons.Extensions;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
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
            .IncludeWarehouseDetails()
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = mapper.Map<List<WarehouseDto>>(pagedWarehouses);

        foreach (var (warehouse, dto) in pagedWarehouses.Zip(mapped))
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Capacity is null)
                continue;

            WarehouseCapacityMapper.Apply(dto, capacityCalculator.Calculate(warehouse));
        }

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }
}