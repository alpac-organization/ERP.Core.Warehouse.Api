using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;
using ERP.Core.Database.Domain.Entities.Inventory;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Handlers;

public class GetSupplyRequestsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSupplyRequestsQuery, PagedResponse<GetSupplyRequestDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<GetSupplyRequestDto>> Handle(GetSupplyRequestsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var branchId = access.Profile.BranchId;

        var supplyRequestsQuery = _unitOfWork.SupplyRequests.Entities
            .Where(sr => sr.BranchId == branchId && sr.DeletedAt == null)
            .AsNoTracking();

        supplyRequestsQuery = ApplyRequestFilters(supplyRequestsQuery, request);

        var totalRecords = await supplyRequestsQuery.CountAsync(ct);

        var supplyRequests = await supplyRequestsQuery
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
        if (!string.IsNullOrEmpty(request.Code)) query = query.Where(sr => sr.Code == request.Code);

        if (request.OutflowStatus.HasValue) query = query.Where(sr => sr.OutflowStatus == request.OutflowStatus);

        if (request.WorkAreaId.HasValue) query = query.Where(sr => sr.WorkAreaId == request.WorkAreaId);

        if (request.CostCenterId.HasValue) query = query.Where(sr => sr.CostCenterId == request.CostCenterId);

        // if (request.BranchId.HasValue) query = query.Where(sr => sr.BranchId == request.BranchId);

        if (request.WarehouseId.HasValue) query = query.Where(sr => sr.WarehouseId == request.WarehouseId);

        if (request.OperationalOrderId.HasValue) query = query.Where(sr => sr.OperationalOrderId == request.OperationalOrderId);

        if (request.RequestedByUserId.HasValue) query = query.Where(sr => sr.RequestedByUserId == request.RequestedByUserId);

        return query;
    }
    #endregion Filtros de busqueda
}