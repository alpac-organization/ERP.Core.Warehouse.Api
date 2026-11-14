using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Inventory;

public static class SupplyRequestSharedHelper
{
    public static async Task<string?> ValidateInventoryAreaAsync(IUnitOfWork unitOfWork, IErrorManager errorManager, Guid workAreaId, CancellationToken ct)
    {
        var allowedArea = await unitOfWork.InventoryAreasPermissions.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(area => area.WorkAreaId == workAreaId && area.IsActive, ct);

        if (allowedArea is null)
            return errorManager.ThrowBadRequest<string>("No tienes permiso para esta acción", "ERP:USER_NOT_AUTOIZED");

        return null;
    }

    public static async Task<(string? BranchCode, string? Error)> GetBranchCodeAsync(IUnitOfWork unitOfWork, IErrorManager errorManager,
        Guid branchId, CancellationToken ct)
    {
        var branchCode = await unitOfWork.Branches.Entities
            .AsNoTracking()
            .Where(branch => branch.Id == branchId && branch.IsActive)
            .Select(branch => branch.BranchCode)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(branchCode))
            return (null, errorManager.ThrowBadRequest<string>(
                "No se encontró el código de sucursal.", "ERP:BRANCH_CODE_NOT_FOUND"));

        return (branchCode, null);
    }

    public static IQueryable<T> ApplyBaseSupplyRequestFilter<T>(this IQueryable<T> query, Guid branchId, string? code,
        OutflowStatus? status) where T : BaseSupplyRequest
    {
        query = query.Where(request =>
            request.BranchId == branchId
            && request.DeletedAt == null
            && (request.OutflowStatus == OutflowStatus.PendingReview
                || request.CreatedAt >= DateTime.UtcNow.AddDays(-7)));

        if (!string.IsNullOrEmpty(code))
            query = query.Where(request => request.Code != null && request.Code.Contains(code));

        if (status.HasValue)
            query = query.Where(request => request.OutflowStatus == status.Value);

        return query;
    }

    public static IQueryable<T> ApplyRequestAccessFilter<T, TResponse>(this IQueryable<T> query, AccessValidationResult<TResponse> access)
        where T : BaseSupplyRequest
    {
        if (access.Role?.RoleType == RoleType.Operator)
        {
            return query.Where(request => request.RequestedByUserId == access.Profile.UserId);
        }

        return query.Where(request => request.WorkAreaId == access.Profile.AreaId);
    }
}