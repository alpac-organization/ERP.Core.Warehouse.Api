using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Handlers;

public class GetFuelRequestsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetFuelRequestsQuery, PagedResponse<GetFuelRequestDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<GetFuelRequestDto>> Handle(GetFuelRequestsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor or RoleType.Operator))
            return _errorManager.ThrowUnauthorized<PagedResponse<GetFuelRequestDto>>(
                "No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");

        var branchId = access.Profile.BranchId;

        var fuelRequestsQuery = _unitOfWork.FuelRequests.Entities
            .ApplyBaseSupplyRequestFilter(branchId, request.Code, request.Status)
            .AsNoTracking();

        fuelRequestsQuery = ApplyRequestFilters(fuelRequestsQuery, request);
        fuelRequestsQuery = fuelRequestsQuery.ApplyRequestAccessFilter(access);

        var totalRecords = await fuelRequestsQuery.CountAsync(ct);

        var fuelRequests = await fuelRequestsQuery
            .Include(fr => fr.User)
            .Include(fr => fr.WorkArea)
            .Include(fr => fr.Machinery)
            .Include(fr => fr.ProductStock)
                .ThenInclude(ps => ps.Product)
            .Include(fr => fr.UnitMeasure)
            .OrderByDescending(fr => fr.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var fuelRequestsMapped = mapper.Map<List<GetFuelRequestDto>>(fuelRequests);

        return new PagedResponse<GetFuelRequestDto>(
            fuelRequestsMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }

    private static IQueryable<FuelRequest> ApplyRequestFilters(IQueryable<FuelRequest> query,
        GetFuelRequestsQuery request)
    {
        if (request.MachineryId.HasValue) query = query.Where(fr => fr.MachineryId == request.MachineryId);

        if (request.ProductStockId.HasValue) query = query.Where(fr => fr.ProductStockId == request.ProductStockId);

        return query;
    }
}