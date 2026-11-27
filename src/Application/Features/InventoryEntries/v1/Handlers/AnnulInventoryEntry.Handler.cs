using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Handlers
{
    public class AnnulInventoryEntryHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper)
        : BaseValidatorHandler<AnnulInventoryEntryCommand, InventoryEntryDto>(unitOfWork, errorManager)
    {
        public override async Task<InventoryEntryDto> Handle(AnnulInventoryEntryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var roleType = access.Role?.RoleType;
            var isAdministrator = roleType == RoleType.Administrator;
            var isManager = roleType == RoleType.Manager;

            if (!isAdministrator && !isManager)
            {
                return _errorManager.ThrowUnauthorized<InventoryEntryDto>(
                    "Solo gerentes o administradores pueden anular una entrada de inventario.",
                    "ERP:INVALID_ACCESS");
            }

            var inventoryEntry = await _unitOfWork.InventoryEntries.Entities
                .Include(entry => entry.Branch)
                .Include(entry => entry.WorkArea)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(order => order.PurchaseRequest)
                        .ThenInclude(purchaseRequest => purchaseRequest.WorkArea)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    entry => entry.Id == request.InventoryEntryId && entry.DeletedAt == null,
                    cancellationToken);

            if (inventoryEntry is null)
            {
                return _errorManager.ThrowNotFound<InventoryEntryDto>(
                    "La entrada de inventario no fue encontrada.",
                    "ERP:INVENTORY_ENTRY_NOT_FOUND");
            }

            if (inventoryEntry.Branch is not null && inventoryEntry.Branch.CompanyId != request.CompanyId)
            {
                return _errorManager.ThrowNotFound<InventoryEntryDto>(
                    "La entrada de inventario no fue encontrada.",
                    "ERP:INVENTORY_ENTRY_NOT_FOUND");
            }

            if (!isAdministrator && inventoryEntry.BranchId != access.Profile.BranchId)
            {
                return _errorManager.ThrowUnauthorized<InventoryEntryDto>(
                    "No tiene acceso para anular entradas de inventario de otra sucursal.",
                    "ERP:INVALID_BRANCH_ACCESS");
            }

            if (inventoryEntry.EntryStatus == EntryStatus.CancelEntry)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La entrada de inventario ya se encuentra anulada.",
                    "ERP:INVENTORY_ENTRY_ALREADY_CANCELED");
            }

            if (inventoryEntry.EntryStatus != EntryStatus.PendingReview)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La entrada de inventario ya fue procesada y no puede ser anulada.",
                    "ERP:INVENTORY_ENTRY_ALREADY_PROCESSED");
            }

            var trimmedReason = request.Reason.Trim();
            var now = DateTime.UtcNow;

            inventoryEntry.EntryStatus = EntryStatus.CancelEntry;
            inventoryEntry.Observations = trimmedReason;

            var purchaseRequest = inventoryEntry.PurchaseOrder?.PurchaseRequest;
            if (purchaseRequest is not null && purchaseRequest.DeletedAt == null)
            {
                purchaseRequest.RequestStatus = PurchaseRequestStatus.Canceled;
                purchaseRequest.AnnulledByUserId = access.User.Id;

                PurchaseRequestReturnHistory.Record(
                    purchaseRequest,
                    trimmedReason,
                    AnnulmentScope.FullProcess,
                    PurchaseRequestReturnHistory.InventoryEntrySource,
                    access.User,
                    now);

                await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<InventoryEntryDto>(inventoryEntry);
        }
    }
}
