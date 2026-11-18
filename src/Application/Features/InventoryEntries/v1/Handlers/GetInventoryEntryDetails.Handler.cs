using System.Text.Json;

using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Handlers
{
    public class GetInventoryEntryDetailsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
        : BaseValidatorHandler<GetInventoryEntryDetailsQuery, InventoryEntryDetailDto>(unitOfWork, errorManager)
    {
        public override async Task<InventoryEntryDetailDto> Handle(GetInventoryEntryDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            IQueryable<InventoryEntry> inventoryEntriesQuery = _unitOfWork.InventoryEntries.Entities
                .AsNoTracking()
                .AsSplitQuery()
                .Include(entry => entry.Branch)
                .Include(entry => entry.WorkArea)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(purchaseOrder => purchaseOrder.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                .Include(entry => entry.PurchaseOrder.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure);

            var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

            if (!isAdministrator)
            {
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(entry => entry.BranchId == access.Profile.BranchId);
            }

            var inventoryEntry = await inventoryEntriesQuery
                .FirstOrDefaultAsync(entry => entry.Id == request.InventoryEntryId && entry.DeletedAt == null, cancellationToken);

            if (inventoryEntry is null)
            {
                return _errorManager.ThrowNotFound<InventoryEntryDetailDto>(
                    "La entrada de inventario no fue encontrada.",
                    "ERP:INVENTORY_ENTRY_NOT_FOUND");
            }

            var detail = mapper.Map<InventoryEntryDetailDto>(inventoryEntry);

            if (!string.IsNullOrWhiteSpace(inventoryEntry.AdditionalData))
            {
                try
                {
                    var additionalData = JsonSerializer.Deserialize<PurchaseOrderEntryAdditionalData>(
                        inventoryEntry.AdditionalData,
                        JsonManager.SnakeCaseOptions);

                    if (additionalData is not null)
                    {
                        detail.PurchaseOrderId = additionalData.PurchaseOrderId;
                        detail.SupplierId = additionalData.SupplierId;
                        detail.SupplierName = additionalData.SupplierName;
                        detail.Items = additionalData.Items
                            .Select(item => new InventoryEntryItemDetailDto
                            {
                                PurchaseOrderItemId = item.PurchaseOrderItemId,
                                PurchaseRequestItemId = item.PurchaseRequestItemId,
                                QuotationId = item.QuotationId,
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                ProductCode = item.ProductCode,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                UnitMeasureId = item.UnitMeasureId,
                                UnitMeasureName = item.UnitMeasureName,
                                UnitMeasureSymbol = item.UnitMeasureSymbol,
                                SupplierId = item.SupplierId,
                                SupplierName = item.SupplierName,
                            })
                            .ToList();
                    }
                }
                catch (JsonException)
                {
                    // Si el additional_data no tiene el formato esperado, se mantiene la información base.
                }
            }

            return detail;
        }
    }
}
