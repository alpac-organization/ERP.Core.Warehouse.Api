using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;

using OutputEntity = ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Handlers;

public class GetInventoryOutputsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetInventoryOutputsQuery, PagedResponse<GetInventoryOutputDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<GetInventoryOutputDto>> Handle(GetInventoryOutputsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor))
            return _errorManager.ThrowUnauthorized<PagedResponse<GetInventoryOutputDto>>(
                "No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");

        var branchId = access.Profile.BranchId;

        var outputsQuery = _unitOfWork.InventoryOutputs.Entities
            .Where(output => output.BranchId == branchId && output.DeletedAt == null)
            .AsNoTracking();

        outputsQuery = ApplyRequestFilters(outputsQuery, request);

        var totalRecords = await outputsQuery.CountAsync(ct);

        var outputs = await outputsQuery
            .Include(output => output.Branch)
            .Include(output => output.WorkArea)
            .Include(output => output.CostCenter)
            .Include(output => output.SupplyRequest)
            .Include(output => output.FuelRequest)
            .Include(output => output.ReasonsInventoryRemoval)
            .OrderByDescending(output => output.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var outputsMapped = mapper.Map<List<GetInventoryOutputDto>>(outputs);

        return new PagedResponse<GetInventoryOutputDto>(
            outputsMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }

    private static IQueryable<OutputEntity> ApplyRequestFilters(
        IQueryable<OutputEntity> query,
        GetInventoryOutputsQuery request)
    {
        if (!string.IsNullOrEmpty(request.OutputCode))
            query = query.Where(output => output.Code != null && output.Code.Contains(request.OutputCode));

        if (!string.IsNullOrEmpty(request.RequestType) &&
            request.RequestType.Equals("Supply", StringComparison.OrdinalIgnoreCase))
            query = query.Where(output => output.SupplyRequestId.HasValue);

        if (!string.IsNullOrEmpty(request.RequestType) &&
            request.RequestType.Equals("Fuel", StringComparison.OrdinalIgnoreCase))
            query = query.Where(output => output.FuelRequestId.HasValue);

        if (request.Status.HasValue)
            query = query.Where(output => output.Status == request.Status.Value);

        if (!string.IsNullOrEmpty(request.SupplyRequestCode))
            query = query.Where(output => output.SupplyRequest != null
                && output.SupplyRequest.Code != null
                && output.SupplyRequest.Code.Contains(request.SupplyRequestCode));

        if (!string.IsNullOrEmpty(request.FuelRequestCode))
            query = query.Where(output => output.FuelRequest != null
                && output.FuelRequest.Code != null
                && output.FuelRequest.Code.Contains(request.FuelRequestCode));

        if (request.ReasonInventoryRemovalId.HasValue)
            query = query.Where(output => output.ReasonsInventoryRemovalId == request.ReasonInventoryRemovalId.Value);

        return query;
    }
}