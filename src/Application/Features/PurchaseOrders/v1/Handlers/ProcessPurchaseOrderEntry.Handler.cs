using System.Text.Json;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class ProcessPurchaseOrderEntryHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        ICodeGenerator codeGenerator)
        : BaseValidatorHandler<ProcessPurchaseOrderEntryCommand, InventoryEntryDto>(unitOfWork, errorManager)
    {
        public override async Task<InventoryEntryDto> Handle(ProcessPurchaseOrderEntryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "No tienes permiso para procesar la entrada de inventario de una orden de compra.",
                    "ERP:INVALID_ACCESS");
            }

            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .AsNoTracking()
                .AsSplitQuery()
                .Include(order => order.Supplier)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.Branch)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.WorkArea)
                .Include(order => order.PurchaseOrderItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.UnitMeasure)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(quotation =>
                            quotation.IsActive &&
                            quotation.DeletedAt == null &&
                            quotation.IsAcceptedForPurchase))
                            .ThenInclude(quotation => quotation.Supplier)
                .FirstOrDefaultAsync(order => order.Id == request.PurchaseOrderId && order.DeletedAt == null, cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowNotFound<InventoryEntryDto>(
                    "La orden de compra no fue encontrada.",
                    "ERP:PURCHASE_ORDER_NOT_FOUND");
            }

            if (purchaseOrder.PurchaseRequest is null)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La orden de compra no tiene una solicitud de compra asociada.",
                    "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            if (purchaseOrder.PurchaseRequest.RequestType != PurchaseRequestType.Requisition)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Solo se puede dar entrada a inventario para requisiciones de compra.",
                    "ERP:INVALID_PURCHASE_REQUEST_TYPE");
            }

            if (!purchaseOrder.SupplierId.HasValue)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La orden de compra no tiene un proveedor seleccionado.",
                    "ERP:SUPPLIER_NOT_SELECTED");
            }

            if (purchaseOrder.PurchaseOrderItems.Count == 0)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La orden de compra no tiene productos para dar entrada a inventario.",
                    "ERP:PURCHASE_ORDER_WITHOUT_ITEMS");
            }

            if (purchaseOrder.PurchaseOrderItems.Any(item =>
                    item.Product is null ||
                    item.Product.ProductUsageType != ProductUsageType.Operational)
                )
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Solo se puede dar entrada a inventario para productos operativos.",
                    "ERP:INVALID_PRODUCT_USAGE_TYPE");
            }

            var acceptedQuotations = purchaseOrder.PurchaseRequest.PurchaseRequestItems
                .SelectMany(item => item.Quotations)
                .Where(quotation => quotation.SupplierId == purchaseOrder.SupplierId)
                .GroupBy(quotation => quotation.PurchaseRequestItemId)
                .ToDictionary(group => group.Key, group => group.First());

            if (purchaseOrder.PurchaseOrderItems.Any(item =>
                    !item.PurchaseRequestItemId.HasValue ||
                    !acceptedQuotations.ContainsKey(item.PurchaseRequestItemId.Value)))
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Cada producto debe tener una cotización aprobada del proveedor seleccionado.",
                    "ERP:ACCEPTED_QUOTATION_NOT_FOUND");
            }

            var alreadyProcessed = await _unitOfWork.InventoryEntries.Entities
                .AsNoTracking()
                .AnyAsync(entry =>
                    entry.PurchaseRequestId == purchaseOrder.PurchaseRequestId &&
                    entry.AdditionalData != null &&
                    entry.AdditionalData.Contains(purchaseOrder.Id.ToString()!),
                    cancellationToken);

            if (alreadyProcessed)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La orden de compra ya fue procesada para entrada de inventario.",
                    "ERP:INVENTORY_ENTRY_ALREADY_EXISTS");
            }

            var branchCode = purchaseOrder.PurchaseRequest.Branch?.BranchCode ?? string.Empty;
            var (isCodeGenerated, code) = await codeGenerator.GenerateCodeToInventoryPendingInputs(branchCode, cancellationToken);

            if (!isCodeGenerated || string.IsNullOrWhiteSpace(code))
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "No se pudo generar el código de la entrada de inventario.",
                    "ERP:INVENTORY_ENTRY_CODE_GENERATION_ERROR");
            }

            var supplierName = purchaseOrder.Supplier?.SuppliersLegalName
                ?? purchaseOrder.Supplier?.CommercialName;

            var additionalData = new PurchaseOrderEntryAdditionalData
            {
                PurchaseOrderId = purchaseOrder.Id,
                SupplierId = purchaseOrder.SupplierId,
                SupplierName = supplierName,
                Items = purchaseOrder.PurchaseOrderItems
                    .Select(item =>
                    {
                        var quotation = acceptedQuotations[item.PurchaseRequestItemId!.Value];

                        return new PurchaseOrderEntryAdditionalItem
                        {
                            PurchaseOrderItemId = item.Id,
                            PurchaseRequestItemId = item.PurchaseRequestItemId,
                            QuotationId = quotation.Id,
                            ProductId = item.ProductId,
                            ProductName = item.Product?.ProductName,
                            ProductCode = item.Product?.Code,
                            Quantity = item.Quantity,
                            UnitPrice = quotation.PriceUnit,
                            UnitMeasureId = item.Product?.UnitMeasureId ?? Guid.Empty,
                            UnitMeasureName = item.Product?.UnitMeasure?.Name,
                            UnitMeasureSymbol = item.Product?.UnitMeasure?.Symbol,
                            SupplierId = purchaseOrder.SupplierId,
                            SupplierName = supplierName,
                        };
                    })
                    .ToList(),
            };

            var inventoryEntry = new InventoryEntry
            {
                Id = Guid.NewGuid(),
                Code = code,
                EntryType = EntryType.Buy,
                EntryStatus = EntryStatus.PendingReview,
                Observations = request.Comments,
                BranchId = purchaseOrder.PurchaseRequest.BranchId,
                WorkAreaId = purchaseOrder.PurchaseRequest.AreaId,
                PurchaseRequestId = purchaseOrder.PurchaseRequestId,
                AdditionalData = JsonSerializer.Serialize(additionalData, JsonManager.SnakeCaseOptions),
            };

            await _unitOfWork.InventoryEntries.RegisterInventoryEntry(inventoryEntry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new InventoryEntryDto
            {
                InventoryEntryId = inventoryEntry.Id,
                Code = inventoryEntry.Code,
                EntryStatus = inventoryEntry.EntryStatus,
                BranchInformation = new BranchInformation
                {
                    BranchId = purchaseOrder.PurchaseRequest.BranchId,
                    BranchCode = purchaseOrder.PurchaseRequest.Branch?.BranchCode,
                    BranchName = purchaseOrder.PurchaseRequest.Branch?.BranchName,
                    CompanyAlias = purchaseOrder.PurchaseRequest.Branch?.CompanyAlias,
                },
            };
        }
    }
}
