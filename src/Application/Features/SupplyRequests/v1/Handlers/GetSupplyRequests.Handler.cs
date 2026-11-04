using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Handlers;

public class GetSupplyRequestsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSupplyRequestsQuery, PagedResponse<GetSupplyRequestDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<GetSupplyRequestDto>> Handle(GetSupplyRequestsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor))
            return _errorManager.ThrowUnauthorized<PagedResponse<GetSupplyRequestDto>>(
                "No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");

        var branchId = access.Profile.BranchId;

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var supplyRequestsQuery = _unitOfWork.SupplyRequests.Entities
            .Where(sr => sr.BranchId == branchId
                      && sr.DeletedAt == null
                      && (sr.OutflowStatus == OutflowStatus.PendingReview
                          || sr.CreatedAt >= sevenDaysAgo))
            .AsNoTracking();

        supplyRequestsQuery = ApplyRequestFilters(supplyRequestsQuery, request);

        var totalRecords = await supplyRequestsQuery.CountAsync(ct);

        var supplyRequests = await supplyRequestsQuery
            .Include(sr => sr.User)
            .Include(sr => sr.SupplyRequestItems)
            .Include(sr => sr.OperationalOrder)
            .Include(sr => sr.WorkArea)
            .Include(sr => sr.Warehouse)
            .OrderByDescending(sr => sr.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var supplyRequestsMapped = mapper.Map<List<GetSupplyRequestDto>>(supplyRequests);

        return new PagedResponse<GetSupplyRequestDto>(
            supplyRequestsMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );

    }

    #region Filtros de busqueda
    private static IQueryable<SupplyRequest> ApplyRequestFilters(IQueryable<SupplyRequest> query,
        GetSupplyRequestsQuery request)
    {
        if (!string.IsNullOrEmpty(request.Code)) query = query.Where(sr => sr.Code != null && sr.Code.Contains(request.Code));

        if (request.Status.HasValue) query = query.Where(sr => sr.OutflowStatus == request.Status);

        if (request.BranchId.HasValue) query = query.Where(sr => sr.BranchId == request.BranchId);

        if (request.ReasonUse.HasValue) query = query.Where(sr => sr.SupplyRequestItems.Any(i => i.ReasonUse == request.ReasonUse));

        if (!string.IsNullOrEmpty(request.PoCode)) query = query.Where(sr => sr.OperationalOrder.PoCode != null && sr.OperationalOrder.PoCode.Contains(request.PoCode));

        return query;
    }
    #endregion Filtros de busqueda
}