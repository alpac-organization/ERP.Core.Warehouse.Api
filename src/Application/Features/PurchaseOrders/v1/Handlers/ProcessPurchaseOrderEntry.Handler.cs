using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class ProcessPurchaseOrderEntryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ICodeGenerator _codeGenerator) : BaseValidatorHandler<ProcessPurchaseOrderEntryCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(ProcessPurchaseOrderEntryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<Unit>("No tienes permiso para procesar la entrada de inventario de una orden de compra.", "ERP:INVALID_ACCESS");
            }

            // PurchaseRequest => PurchaseOrder => InventoryEntry...

            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .AsSplitQuery()
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchase => purchase.Branch)
                .Where(order => order.IsActive)
                .Where(order => order.Status == PurchaseOrderStatus.Pending)
                .Where(order => order.Id == request.PurchaseOrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return  _errorManager.ThrowBadRequest<Unit>("La orden de compra seleccionada no existe, o no se encuentra en estado pendiente", "ERP:01");
            }

            var branchCode = purchaseOrder.PurchaseRequest.Branch?.BranchCode ?? string.Empty;
            var (IsSucceded, Code) = await _codeGenerator.GenerateCodeToInventoryPendingInputs(branchCode, cancellationToken);

            if (IsSucceded is false)
            {
                return _errorManager.ThrowBadRequest<Unit>("Ocurrio el generar un codigo para la entrada de inventario","ERP:01");
            }

            var inventoryEntryEntity = InventoryEntryMapper.ToInventoryEntity(request);

            inventoryEntryEntity.BranchId = access.Profile.BranchId;
            inventoryEntryEntity.WorkAreaId = access.Profile.AreaId;
            inventoryEntryEntity.PurchaseOrderId = purchaseOrder.Id;
            inventoryEntryEntity.Code = Code;

            await _unitOfWork.InventoryEntries.RegisterInventoryEntry(inventoryEntryEntity);

            purchaseOrder.Status = PurchaseOrderStatus.Generated;
            await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
