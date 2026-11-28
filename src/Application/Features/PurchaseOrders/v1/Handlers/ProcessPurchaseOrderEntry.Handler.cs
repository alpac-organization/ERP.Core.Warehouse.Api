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
                .Include(order => order.PurchaseOrderItems)
                    .ThenInclude(item => item.Product)
                .Where(order => order.IsActive)
                .Where(order => order.Status == PurchaseOrderStatus.Pending)
                .Where(order => order.Id == request.PurchaseOrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return  _errorManager.ThrowBadRequest<Unit>("La orden de compra seleccionada no existe, o no se encuentra en estado pendiente", "ERP:01");
            }

            if (purchaseOrder.PurchaseRequest is null ||
                purchaseOrder.PurchaseRequest.RequestType != PurchaseRequestType.Requisition ||
                purchaseOrder.PurchaseRequest.Destination != DestinationRequest.ServiceOrder)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Solo las órdenes de compra de requisiciones operacionales pueden dar entrada a inventario.",
                    "ERP:NON_OPERATIONAL_PURCHASE_REQUEST");
            }

            var activeOrderItems = purchaseOrder.PurchaseOrderItems
                .Where(item => item.DeletedAt == null)
                .ToList();

            if (activeOrderItems.Count == 0 ||
                activeOrderItems.Any(item =>
                    item.Product is null ||
                    item.Product.DeletedAt != null ||
                    item.Product.ProductUsageType != ProductUsageType.Operational))
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "No se puede dar entrada a inventario a órdenes de compra que no sean de productos operativos.",
                    "ERP:NON_OPERATIONAL_PRODUCTS");
            }

            var branchCode = (purchaseOrder.PurchaseRequest.Branch?.BranchCode ?? string.Empty).ToUpperInvariant();
            var (IsSucceded, Code) = await _codeGenerator.GenerateCodeToInventoryPendingInputs(branchCode, cancellationToken);

            if (IsSucceded is false)
            {
                return _errorManager.ThrowBadRequest<Unit>("Ocurrio el generar un codigo para la entrada de inventario","ERP:01");
            }

            var inventoryEntryEntity = InventoryEntryMapper.ToInventoryEntity(request);

            inventoryEntryEntity.BranchId = purchaseOrder.PurchaseRequest.BranchId;
            inventoryEntryEntity.WorkAreaId = purchaseOrder.PurchaseRequest.AreaId;
            inventoryEntryEntity.PurchaseOrderId = purchaseOrder.Id;
            inventoryEntryEntity.Code = Code?.ToUpperInvariant();

            await _unitOfWork.InventoryEntries.RegisterInventoryEntry(inventoryEntryEntity);

            purchaseOrder.Status = PurchaseOrderStatus.Generated;
            await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
