using System.Text.Json;

using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Shopping;
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
                    .ThenInclude(purchaseOrder => purchaseOrder.Supplier)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(purchaseOrder => purchaseOrder.PurchaseOrderItems)
                        .ThenInclude(orderItem => orderItem.Product)
                            .ThenInclude(product => product.CostCenter)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(purchaseOrder => purchaseOrder.PurchaseOrderItems)
                        .ThenInclude(orderItem => orderItem.Product)
                            .ThenInclude(product => product.UnitMeasure)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(purchaseOrder => purchaseOrder.PurchaseRequest)
                        .ThenInclude(purchaseRequest => purchaseRequest.CostCenter)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(purchaseOrder => purchaseOrder.PurchaseRequest)
                        .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                            .ThenInclude(item => item.Product)
                                .ThenInclude(product => product.CostCenter)
                .Include(entry => entry.PurchaseOrder.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(entry => entry.PurchaseOrder.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(quotation =>
                            quotation.IsActive &&
                            quotation.DeletedAt == null &&
                            quotation.IsAcceptedForPurchase))
                            .ThenInclude(quotation => quotation.Supplier)
                .Include(entry => entry.PurchaseOrder.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(quotation =>
                            quotation.IsActive &&
                            quotation.DeletedAt == null &&
                            quotation.IsAcceptedForPurchase))
                            .ThenInclude(quotation => quotation.SupplierProduct);

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

            detail.InvoiceNumber = inventoryEntry.InvoiceNumber;
            detail.InvoiceDate = inventoryEntry.InvoiceDate;

            var purchaseOrder = inventoryEntry.PurchaseOrder;
            var purchaseRequest = purchaseOrder?.PurchaseRequest;
            var supplierName = purchaseOrder?.Supplier?.SuppliersLegalName
                ?? purchaseOrder?.Supplier?.CommercialName;

            detail.PurchaseOrderId = purchaseOrder?.Id ?? inventoryEntry.PurchaseOrderId;
            detail.SupplierId = purchaseOrder?.SupplierId;
            detail.SupplierName = supplierName;

            var orderItemsById = (purchaseOrder?.PurchaseOrderItems ?? [])
                .Where(item => item.DeletedAt == null)
                .ToDictionary(item => item.Id);

            PurchaseOrderEntryAdditionalData? additionalData = null;

            if (!string.IsNullOrWhiteSpace(inventoryEntry.AdditionalData))
            {
                try
                {
                    additionalData = JsonSerializer.Deserialize<PurchaseOrderEntryAdditionalData>(
                        inventoryEntry.AdditionalData,
                        JsonManager.SnakeCaseOptions);
                }
                catch (JsonException)
                {
                    // Si el additional_data no tiene el formato esperado, se usa la información relacional.
                }
            }

            if (additionalData is not null)
            {
                detail.PurchaseOrderId = additionalData.PurchaseOrderId ?? detail.PurchaseOrderId;
                detail.SupplierId = additionalData.SupplierId ?? detail.SupplierId;
                detail.SupplierName = additionalData.SupplierName ?? detail.SupplierName;
                detail.ExchangeRateApplied = additionalData.ExchangeRateApplied;
                detail.ExchangeRateDate = additionalData.ExchangeRateDate;
                detail.EvidenceImages = (additionalData.EvidenceImages ?? [])
                    .Select(image => new InventoryEntryEvidenceImageDto
                    {
                        ImageId = image.ImageId,
                        ImageUrl = image.ImageUrl
                    })
                    .ToList();
            }

            if (additionalData?.Items is { Count: > 0 })
            {
                var missingCostCenterIds = additionalData.Items
                    .Where(item => item.CostCenterId.HasValue && string.IsNullOrWhiteSpace(item.CostCenterName))
                    .Select(item => item.CostCenterId!.Value)
                    .Distinct()
                    .ToList();

                var extraCostCenters = missingCostCenterIds.Count > 0
                    ? await _unitOfWork.CostCenters.Entities
                        .AsNoTracking()
                        .Where(cc => missingCostCenterIds.Contains(cc.Id))
                        .ToDictionaryAsync(cc => cc.Id, cancellationToken)
                    : new Dictionary<Guid, CostCenter>();

                detail.Items = additionalData.Items
                    .Select(item =>
                    {
                        PurchaseOrderItem? orderItem = null;
                        if (item.PurchaseOrderItemId.HasValue)
                        {
                            orderItemsById.TryGetValue(item.PurchaseOrderItemId.Value, out orderItem);
                        }

                        var costCenterInfo = ResolveAdditionalItemCostCenter(
                            item,
                            extraCostCenters,
                            orderItem?.Product?.CostCenter,
                            purchaseRequest?.CostCenter);

                        return new InventoryEntryItemDetailDto
                        {
                            PurchaseOrderItemId = item.PurchaseOrderItemId,
                            PurchaseRequestItemId = item.PurchaseRequestItemId,
                            QuotationId = item.QuotationId,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName ?? orderItem?.Product?.ProductName,
                            ProductCode = item.ProductCode ?? orderItem?.Product?.Code,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Currency = item.OriginalCurrency ?? Currency.NIO,
                            OriginalUnitPrice = item.OriginalUnitPrice,
                            ExchangeRateApplied = item.ExchangeRateApplied,
                            UnitMeasureId = item.UnitMeasureId != Guid.Empty
                                ? item.UnitMeasureId
                                : (orderItem?.Product?.UnitMeasureId ?? Guid.Empty),
                            UnitMeasureName = item.UnitMeasureName ?? orderItem?.Product?.UnitMeasure?.Name,
                            UnitMeasureSymbol = item.UnitMeasureSymbol ?? orderItem?.Product?.UnitMeasure?.Symbol,
                            SupplierId = item.SupplierId ?? detail.SupplierId,
                            SupplierName = item.SupplierName ?? detail.SupplierName,
                            CostCenterId = costCenterInfo?.CostCenterId,
                            CostCenterName = costCenterInfo?.CostCenterName,
                            CostCenterCode = costCenterInfo?.CostCenterCode,
                            CostCenterInformation = costCenterInfo,
                        };
                    })
                    .ToList();
            }
            else if (orderItemsById.Count > 0)
            {
                var kardexCostCenterByProductId = new Dictionary<Guid, CostCenter>();

                if (inventoryEntry.EntryStatus == EntryStatus.ApprovedEntry)
                {
                    var productIds = orderItemsById.Values.Select(i => i.ProductId).Distinct().ToList();
                    var kardexMovements = await _unitOfWork.Kardex.Entities
                        .AsNoTracking()
                        .Include(k => k.CostCenter)
                        .Where(k => k.WorkAreaId == inventoryEntry.WorkAreaId
                                 && k.MovementType == KardexMovementType.Buys
                                 && productIds.Contains(k.ProductId)
                                 && k.DeletedAt == null)
                        .OrderByDescending(k => k.CreatedAt)
                        .ToListAsync(cancellationToken);

                    foreach (var movement in kardexMovements)
                    {
                        if (movement.CostCenter is not null && !kardexCostCenterByProductId.ContainsKey(movement.ProductId))
                        {
                            kardexCostCenterByProductId[movement.ProductId] = movement.CostCenter;
                        }
                    }
                }

                var acceptedQuotations = (purchaseRequest?.PurchaseRequestItems ?? [])
                    .SelectMany(item => item.Quotations)
                    .Where(quotation => purchaseOrder?.SupplierId == null || quotation.SupplierId == purchaseOrder.SupplierId)
                    .GroupBy(quotation => quotation.PurchaseRequestItemId)
                    .ToDictionary(group => group.Key, group => group.First());

                detail.Items = orderItemsById.Values
                    .Select(orderItem =>
                    {
                        Quotation? quotation = null;
                        if (orderItem.PurchaseRequestItemId.HasValue)
                        {
                            acceptedQuotations.TryGetValue(orderItem.PurchaseRequestItemId.Value, out quotation);
                        }

                        kardexCostCenterByProductId.TryGetValue(orderItem.ProductId, out var kardexCostCenter);
                        var resolvedCostCenter = kardexCostCenter
                            ?? orderItem.Product?.CostCenter
                            ?? purchaseRequest?.CostCenter;
                        var costCenterInfo = MapCostCenter(resolvedCostCenter);
                        var itemCurrency = quotation?.SupplierProduct?.Currency ?? Currency.NIO;

                        return new InventoryEntryItemDetailDto
                        {
                            PurchaseOrderItemId = orderItem.Id,
                            PurchaseRequestItemId = orderItem.PurchaseRequestItemId,
                            QuotationId = quotation?.Id,
                            ProductId = orderItem.ProductId,
                            ProductName = orderItem.Product?.ProductName,
                            ProductCode = orderItem.Product?.Code,
                            Quantity = orderItem.Quantity,
                            UnitPrice = orderItem.UnitPrice,
                            Currency = itemCurrency,
                            OriginalUnitPrice = orderItem.UnitPrice,
                            UnitMeasureId = orderItem.Product?.UnitMeasureId ?? Guid.Empty,
                            UnitMeasureName = orderItem.Product?.UnitMeasure?.Name,
                            UnitMeasureSymbol = orderItem.Product?.UnitMeasure?.Symbol,
                            SupplierId = quotation?.SupplierId ?? detail.SupplierId,
                            SupplierName = quotation?.Supplier?.SuppliersLegalName
                                ?? quotation?.Supplier?.CommercialName
                                ?? detail.SupplierName,
                            CostCenterId = costCenterInfo?.CostCenterId,
                            CostCenterName = costCenterInfo?.CostCenterName,
                            CostCenterCode = costCenterInfo?.CostCenterCode,
                            CostCenterInformation = costCenterInfo,
                        };
                    })
                    .ToList();
            }

            return detail;
        }

        private static CostCenterInformation? ResolveAdditionalItemCostCenter(
            PurchaseOrderEntryAdditionalItem item,
            IReadOnlyDictionary<Guid, CostCenter> extraCostCenters,
            CostCenter? productCostCenter,
            CostCenter? requestCostCenter)
        {
            if (item.CostCenterId.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(item.CostCenterName))
                {
                    return new CostCenterInformation
                    {
                        CostCenterId = item.CostCenterId.Value,
                        CostCenterName = item.CostCenterName,
                        CostCenterCode = item.CostCenterCode ?? string.Empty,
                        Description = item.CostCenterDescription,
                        CoilCode = item.CostCenterCoilCode ?? 0,
                    };
                }

                if (extraCostCenters.TryGetValue(item.CostCenterId.Value, out var fetched))
                {
                    return MapCostCenter(fetched);
                }
            }

            return MapCostCenter(productCostCenter ?? requestCostCenter);
        }

        private static CostCenterInformation? MapCostCenter(CostCenter? costCenter)
        {
            if (costCenter is null)
            {
                return null;
            }

            return new CostCenterInformation
            {
                CostCenterId = costCenter.Id,
                CostCenterName = costCenter.CostCenterName,
                CostCenterCode = costCenter.CostCenterCode,
                Description = costCenter.Description,
                CoilCode = costCenter.CoilCode,
            };
        }
    }
}
