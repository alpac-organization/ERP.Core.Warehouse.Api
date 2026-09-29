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

public class GetWarehousesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetWarehousesQuery, PagedResponse<WarehouseDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<WarehouseDto>> Handle(
        GetWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var query = _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(w=>w.DeletedAt == null);

        query = ApplyFilters(query,request);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
                    .OrderByDescending(w=> w.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
        var mapped = mapper.Map<List<WarehouseDto>>(items);

        return new PagedResponse<WarehouseDto>(
            mapped,
            request.PageNumber,
            request.PageSize,
            total
        );
    }

    private static IQueryable<WarehouseEntity> ApplyFilters(IQueryable<WarehouseEntity> query, GetWarehousesQuery request)
    {
        if (request.IsActive.HasValue)
            query = query.Where(ware => ware.IsActive == request.IsActive.Value);


        if (!string.IsNullOrWhiteSpace(request.WarehouseCode))
            query = query.Where(ware => ware.Code == request.WarehouseCode);

        if (request.WarehouseType.HasValue)
            query = query.Where(ware => ware.WarehouseType == request.WarehouseType.Value);

        return query;
    }
}