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

        return await WarehousePagedQuery.ExecuteAsync(
            filteredQuery, request, mapper, capacityCalculator, cancellationToken);
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