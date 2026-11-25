using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using KardexEntity = ERP.Core.Database.Domain.Entities.Inventory.Kardex;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public abstract class OutputReportHandlerBase<TQuery, TResponse>(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : InventoryReportHandlerBase<TQuery, TResponse>(unitOfWork, errorManager)
    where TQuery : OutputReportQueryBase, IRequest<TResponse>
    where TResponse : class
{
    protected TResponse? ValidateAndResolveBranch(
        TQuery request,
        bool requireCategory,
        AccessValidationResult<TResponse> access,
        out Guid branchId)
    {
        branchId = Guid.Empty;

        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<TResponse>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<TResponse>("El periodo no es valido.", "ERP:010");

        if (requireCategory && request.CategoryId is not { Count: > 0 })
            return _errorManager.ThrowBadRequest<TResponse>("Debe enviar al menos una categoria.", "ERP:009");

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (!isAdministrator && access.Profile == null)
            return _errorManager.ThrowBadRequest<TResponse>("Perfil no encontrado.", "ERP:011");

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<TResponse>("Debe especificar la sucursal.", "ERP:008");

        branchId = isAdministrator ? request.BranchId!.Value : access.Profile.BranchId;
        return null;
    }

    protected async Task<List<KardexEntity>> LoadKardexAsync(TQuery request, Guid branchId, CancellationToken ct)
    {
        var query = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Include(k => k.Product)
                .ThenInclude(p => p.Category)
            .Include(k => k.CostCenter)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.Branch)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.SupplyRequest)
                    .ThenInclude(s => s!.SupplyRequestItems)
                        .ThenInclude(i => i.ProductStock)
                            .ThenInclude(ps => ps!.Product)
                                .ThenInclude(p => p!.Category)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.SupplyRequest)
                    .ThenInclude(s => s!.Equipment)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.SupplyRequest)
                    .ThenInclude(s => s!.OperationalOrder)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.SupplyRequest)
                    .ThenInclude(s => s!.Customer)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.FuelRequest)
                    .ThenInclude(f => f!.ProductStock)
                        .ThenInclude(ps => ps!.Product)
                            .ThenInclude(p => p!.Category)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.FuelRequest)
                    .ThenInclude(f => f!.Equipment)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.FuelRequest)
                    .ThenInclude(f => f!.Customer)
            .Where(k => k.MovementType == KardexMovementType.Exit)
            .Where(k => k.DeletedAt == null)
            .Where(k => k.InventoryOutput != null)
            .Where(k => k.InventoryOutput!.Status == InventoryOutputsStatus.Dispatched)
            .Where(k => k.InventoryOutput!.DeletedAt == null)
            .Where(k => k.InventoryOutput!.BranchId == branchId)
            .Where(k => (k.InventoryOutput!.DispatchedAt ?? k.InventoryOutput!.CreatedAt) >= request.From!.Value)
            .Where(k => (k.InventoryOutput!.DispatchedAt ?? k.InventoryOutput!.CreatedAt) <= request.To!.Value.Date.AddDays(1).AddTicks(-1));

        if (request.EquipmentIds is { Count: > 0 })
        {
            query = query.Where(k =>
                (k.InventoryOutput!.SupplyRequest != null && request.EquipmentIds.Contains(k.InventoryOutput.SupplyRequest.EquipmentId ?? Guid.Empty)) ||
                (k.InventoryOutput!.FuelRequest != null && request.EquipmentIds.Contains(k.InventoryOutput.FuelRequest.EquipmentId)));
        }

        if (request.CategoryId is { Count: > 0 })
        {
            query = query.Where(k => k.Product != null && request.CategoryId.Contains(k.Product.CategoryId));
        }

        if (request.ProductIds is { Count: > 0 })
        {
            query = query.Where(k => request.ProductIds.Contains(k.ProductId));
        }

        return await query.ToListAsync(ct);
    }
}
