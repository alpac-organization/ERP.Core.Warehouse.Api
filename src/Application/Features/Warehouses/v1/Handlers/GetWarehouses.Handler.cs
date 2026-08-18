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
            .AsNoTracking()
            .Where(ware => ware.ParentWarehouseId == null);

        warehousesQuery = ApplyFilters(warehousesQuery, request);

        var totalRecords = await warehousesQuery.CountAsync(cancellationToken);

        var warehouses = await warehousesQuery
            .OrderBy(ware => ware.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(ware => ware.SubWarehouses.Where(sub => sub.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<WarehouseDto>(
            mapper.Map<List<WarehouseDto>>(warehouses),
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }

    private static IQueryable<WarehouseEntity> ApplyFilters(
        IQueryable<WarehouseEntity> query,
        GetWarehousesQuery request)
    {
        query = request.IsActive.HasValue
            ? query.Where(ware => ware.IsActive == request.IsActive.Value)
            : query.Where(ware => ware.IsActive);

        if (!string.IsNullOrWhiteSpace(request.BranchCode))
            query = query.Where(ware => ware.Branch.BranchCode == request.BranchCode);

        if (!string.IsNullOrWhiteSpace(request.WarehouseCode))
            query = query.Where(ware => ware.Code == request.WarehouseCode);

        if (request.WarehouseType.HasValue)
            query = query.Where(ware => ware.WarehouseType == request.WarehouseType.Value);

        return query;
    }
}