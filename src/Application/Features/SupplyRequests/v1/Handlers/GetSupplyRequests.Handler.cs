using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
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

        var branchId = access.Profile.BranchId;

        var supplyRequestsQuery = _unitOfWork.SupplyRequests.Entities
            .ApplyBaseSupplyRequestFilter(branchId, request.Code, request.Status)
            .AsNoTracking();

        supplyRequestsQuery = ApplyRequestFilters(supplyRequestsQuery, request);

        var totalRecords = await supplyRequestsQuery.CountAsync(ct);

        var supplyRequests = await supplyRequestsQuery
            .Include(sr => sr.User)
            .Include(sr => sr.SupplyRequestItems)
            .Include(sr => sr.OperationalOrder)
            .Include(sr => sr.WorkArea)
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
        if (request.ReasonUse.HasValue) query = query.Where(sr => sr.SupplyRequestItems.Any(i => i.ReasonUse == request.ReasonUse));

        if (!string.IsNullOrEmpty(request.PoCode)) query = query.Where(sr => sr.OperationalOrder.PoCode != null && sr.OperationalOrder.PoCode.Contains(request.PoCode));

        return query;
    }
    #endregion Filtros de busqueda
}