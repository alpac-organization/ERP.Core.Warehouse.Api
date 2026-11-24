using System.Text.Json;

using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Application.Commons.Utils;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Handlers
{
    public class RegisterInventoryEntryHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IS3StorageService s3StorageService,
        IWeightedAverageCostService weightedAverageCostService)
        : BaseValidatorHandler<RegisterInventoryEntryCommand, InventoryEntryDto>(unitOfWork, errorManager)
    {
        public override async Task<InventoryEntryDto> Handle(RegisterInventoryEntryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<InventoryEntryDto>(
                    "No tiene acceso para realizar esta acción",
                    "ERP:INVALID_ACCESS");
            }

            var inventoryEntry = await _unitOfWork.InventoryEntries.Entities
                .Include(entry => entry.Branch)
                .Include(entry => entry.WorkArea)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(order => order.Supplier)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(order => order.PurchaseRequest)
                        .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                            .ThenInclude(requestItem => requestItem.Quotations.Where(q =>
                                q.IsActive &&
                                q.DeletedAt == null &&
                                q.IsAcceptedForPurchase))
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(order => order.PurchaseOrderItems)
                        .ThenInclude(item => item.Product)
                            .ThenInclude(product => product.UnitMeasure)
                .AsSplitQuery()
                .FirstOrDefaultAsync(entry => entry.Id == request.InventoryEntryId && entry.DeletedAt == null, cancellationToken);

            if (inventoryEntry is null)
            {
                return _errorManager.ThrowNotFound<InventoryEntryDto>(
                    "La entrada de inventario no fue encontrada.",
                    "ERP:INVENTORY_ENTRY_NOT_FOUND");
            }

            if (inventoryEntry.EntryStatus != EntryStatus.PendingReview)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La entrada de inventario ya fue procesada.",
                    "ERP:INVENTORY_ENTRY_ALREADY_PROCESSED");
            }

            var purchaseOrderItems = inventoryEntry.PurchaseOrder?.PurchaseOrderItems
                .Where(item => item.DeletedAt == null)
                .ToDictionary(item => item.Id) ?? [];

            var requestedItemIds = request.Items.Select(item => item.PurchaseOrderItemId).ToList();

            if (requestedItemIds.Count == 0)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Debe registrar al menos un ítem en la entrada.",
                    "ERP:INVENTORY_ENTRY_WITHOUT_ITEMS");
            }

            if (requestedItemIds.Any(itemId => !purchaseOrderItems.ContainsKey(itemId)))
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Uno o más ítems no pertenecen a la orden de compra de la entrada.",
                    "ERP:INVENTORY_ENTRY_ITEM_NOT_IN_PURCHASE_ORDER");
            }

            var costCenterIds = request.Items.Select(item => item.CostCenterId).Distinct().ToList();

            var existingCostCenters = await _unitOfWork.CostCenters.Entities
                .AsNoTracking()
                .Where(costCenter => costCenterIds.Contains(costCenter.Id) && costCenter.IsActive)
                .ToDictionaryAsync(costCenter => costCenter.Id, cancellationToken);

            if (existingCostCenters.Count != costCenterIds.Count)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Uno o más centros de costo no existen o están inactivos.",
                    "ERP:INVALID_COST_CENTER");
            }

            var evidenceImages = new List<EntryEvidenceImage>();

            foreach (var image in request.EvidenceImagesBase64)
            {
                var imageUrl = await s3StorageService.UploadImageAsync(
                    "Warehouse",
                    "InventoryEntries",
                    image,
                    cancellationToken);

                evidenceImages.Add(new EntryEvidenceImage
                {
                    ImageId = Guid.NewGuid(),
                    ImageUrl = imageUrl
                });
            }

            var supplierName = inventoryEntry.PurchaseOrder?.Supplier?.SuppliersLegalName
                ?? inventoryEntry.PurchaseOrder?.Supplier?.CommercialName;

            var acceptedQuotationsByRequestItemId = (inventoryEntry.PurchaseOrder?.PurchaseRequest?.PurchaseRequestItems ?? [])
                .SelectMany(item => item.Quotations)
                .Where(quotation => quotation.SupplierId == inventoryEntry.PurchaseOrder?.SupplierId)
                .GroupBy(quotation => quotation.PurchaseRequestItemId)
                .ToDictionary(group => group.Key, group => group.First());

            var additionalData = DeserializeAdditionalData(inventoryEntry.AdditionalData);
            additionalData.PurchaseOrderId ??= inventoryEntry.PurchaseOrderId;
            additionalData.SupplierId ??= inventoryEntry.PurchaseOrder?.SupplierId;
            additionalData.SupplierName ??= supplierName;
            additionalData.EvidenceImages.Clear();
            additionalData.EvidenceImages.AddRange(evidenceImages);

            var costCenterByOrderItemId = request.Items
                .GroupBy(item => item.PurchaseOrderItemId)
                .ToDictionary(group => group.Key, group => existingCostCenters[group.Last().CostCenterId]);

            additionalData.Items = purchaseOrderItems.Values
                .Select(orderItem =>
                {
                    costCenterByOrderItemId.TryGetValue(orderItem.Id, out var designatedCostCenter);
                    Guid? quotationId = null;

                    if (orderItem.PurchaseRequestItemId.HasValue &&
                        acceptedQuotationsByRequestItemId.TryGetValue(orderItem.PurchaseRequestItemId.Value, out var quotation))
                    {
                        quotationId = quotation.Id;
                    }

                    return new PurchaseOrderEntryAdditionalItem
                    {
                        PurchaseOrderItemId = orderItem.Id,
                        PurchaseRequestItemId = orderItem.PurchaseRequestItemId,
                        QuotationId = quotationId,
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.Product?.ProductName,
                        ProductCode = orderItem.Product?.Code,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        UnitMeasureId = orderItem.Product?.UnitMeasureId ?? Guid.Empty,
                        UnitMeasureName = orderItem.Product?.UnitMeasure?.Name,
                        UnitMeasureSymbol = orderItem.Product?.UnitMeasure?.Symbol,
                        SupplierId = inventoryEntry.PurchaseOrder?.SupplierId,
                        SupplierName = supplierName,
                        CostCenterId = designatedCostCenter?.Id,
                        CostCenterName = designatedCostCenter?.CostCenterName,
                        CostCenterCode = designatedCostCenter?.CostCenterCode,
                        CostCenterDescription = designatedCostCenter?.Description,
                        CostCenterCoilCode = designatedCostCenter?.CoilCode,
                    };
                })
                .ToList();

            inventoryEntry.Observations = string.IsNullOrWhiteSpace(request.Observations)
                ? inventoryEntry.Observations
                : request.Observations.Trim();
            inventoryEntry.InvoiceNumber = request.InvoiceNumber.Trim();
            inventoryEntry.InvoiceDate = request.InvoiceDate;
            inventoryEntry.AdditionalData = JsonSerializer.Serialize(additionalData, JsonManager.SnakeCaseOptions);
            inventoryEntry.EntryStatus = EntryStatus.ApprovedEntry;

            // inventoryEntry.InventoryEntryItems.Clear();

            var registeredAt = DateTime.UtcNow;

            foreach (var requestedItem in request.Items)
            {
                var purchaseOrderItem = purchaseOrderItems[requestedItem.PurchaseOrderItemId];

                var product = purchaseOrderItem.Product;

                if (product is null)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        "Uno de los ítems de la orden de compra no tiene un producto asociado.",
                        "ERP:PURCHASE_ORDER_ITEM_WITHOUT_PRODUCT");
                }

                // inventoryEntry.InventoryEntryItems.Add(new InventoryEntryItem
                // {
                //     Id = Guid.NewGuid(),
                //     InventoryEntryId = inventoryEntry.Id,
                //     PurchaseOrderItemId = purchaseOrderItem.Id,
                //     CostCenterId = requestedItem.CostCenterId
                // });

                var productStock = await _unitOfWork.ProductStocks.Entities
                    .FirstOrDefaultAsync(stock =>
                        stock.ProductId == purchaseOrderItem.ProductId &&
                        stock.CostCenterId == requestedItem.CostCenterId &&
                        stock.DeletedAt == null,
                        cancellationToken);

                var previousQuantity = productStock?.AvailableQuantity ?? 0;
                var previousAverageCost = productStock?.WeightedAverageCost ?? 0;

                var weightedCost = weightedAverageCostService.Calculate(
                    previousQuantity,
                    previousAverageCost,
                    purchaseOrderItem.Quantity,
                    purchaseOrderItem.UnitPrice);

                if (productStock is null)
                {
                    productStock = new ProductStock
                    {
                        Id = Guid.NewGuid(),
                        IsActive = true,
                        MinimumStock = 0,
                        AvailableQuantity = weightedCost.NewQuantity,
                        WeightedAverageCost = weightedCost.NewAverageCost,
                        Status = ProductStockStatus.Stable,
                        UnitMeasureId = product.UnitMeasureId,
                        ProductId = purchaseOrderItem.ProductId,
                        CostCenterId = requestedItem.CostCenterId
                    };

                    await _unitOfWork.ProductStocks.RegisterProductStock(productStock);
                }
                else
                {
                    productStock.AvailableQuantity = weightedCost.NewQuantity;
                    productStock.WeightedAverageCost = weightedCost.NewAverageCost;
                    productStock.Status = ResolveStockStatus(productStock);

                    await _unitOfWork.ProductStocks.UpdateAsync(productStock);
                }

                var totalInput = Math.Round(
                    purchaseOrderItem.Quantity * purchaseOrderItem.UnitPrice,
                    2,
                    MidpointRounding.AwayFromZero);

                var kardex = new Kardex
                {
                    Id = Guid.NewGuid(),
                    KardexType = product.ProductUsageType == ProductUsageType.Operational
                        ? KardexType.Operational
                        : KardexType.Administrative,
                    MovementType = KardexMovementType.Buys,
                    Units = purchaseOrderItem.Quantity,
                    UnitCost = purchaseOrderItem.UnitPrice,
                    TotalInput = totalInput,
                    TotalOutput = 0,
                    TotalUnits = weightedCost.NewQuantity,
                    AverageCost = weightedCost.NewAverageCost,
                    TotalKardex = weightedCost.NewTotalValue,
                    TransactionDate = DateOnly.FromDateTime(registeredAt),
                    TransactionTime = TimeOnly.FromDateTime(registeredAt),
                    ProductId = purchaseOrderItem.ProductId,
                    MovementByUserId = request.UserId,
                    WorkAreaId = inventoryEntry.WorkAreaId,
                    CostCenterId = requestedItem.CostCenterId
                };

                await _unitOfWork.Kardex.RegisterKardexMovement(kardex);
            }

            await _unitOfWork.InventoryEntries.UpdateAsync(inventoryEntry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new InventoryEntryDto
            {
                InventoryEntryId = inventoryEntry.Id,
                CreatedAt = inventoryEntry.CreatedAt,
                Code = inventoryEntry.Code,
                EntryType = inventoryEntry.EntryType,
                EntryStatus = inventoryEntry.EntryStatus,
                PurchaseRequestCode = inventoryEntry.PurchaseOrder?.PurchaseRequest?.Code,
                BranchInformation = new BranchInformation
                {
                    BranchId = inventoryEntry.BranchId,
                    BranchCode = inventoryEntry.Branch?.BranchCode,
                    BranchName = inventoryEntry.Branch?.BranchName,
                    CompanyAlias = inventoryEntry.Branch?.CompanyAlias
                },
                WorkAreaInformation = new WorkAreaInformation
                {
                    WorkAreaId = inventoryEntry.WorkAreaId,
                    WorkAreaName = inventoryEntry.WorkArea?.WorkAreaName
                }
            };
        }

        private static PurchaseOrderEntryAdditionalData DeserializeAdditionalData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PurchaseOrderEntryAdditionalData();
            }

            try
            {
                return JsonSerializer.Deserialize<PurchaseOrderEntryAdditionalData>(json, JsonManager.SnakeCaseOptions)
                    ?? new PurchaseOrderEntryAdditionalData();
            }
            catch (JsonException)
            {
                return new PurchaseOrderEntryAdditionalData();
            }
        }

        private static ProductStockStatus ResolveStockStatus(ProductStock stock)
        {
            if (stock.AvailableQuantity <= 0)
            {
                return ProductStockStatus.Empty;
            }

            return stock.AvailableQuantity <= stock.MinimumStock
                ? ProductStockStatus.LowBalance
                : ProductStockStatus.Stable;
        }
    }
}
