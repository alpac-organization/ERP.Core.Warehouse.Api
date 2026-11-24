using System.Text.Json;

using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Application.Commons.Utils;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
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
        IWeightedAverageCostService weightedAverageCostService,
        IMapper mapper)
        : BaseValidatorHandler<RegisterInventoryEntryCommand, InventoryEntryDto>(unitOfWork, errorManager)
    {
        private const decimal MaxStockQuantityPrecision = 99999.99m;
        private const decimal MaxStockAverageCostPrecision = 999999.99m;

        private static readonly TimeZoneInfo NicaraguaTimeZone =
            TimeZoneInfo.CreateCustomTimeZone("Nicaragua", TimeSpan.FromHours(-6), "Nicaragua", "Nicaragua");

        private sealed record ResolvedEntryLine(
            PurchaseOrderItem OrderItem,
            Product Product,
            CostCenter CostCenter,
            decimal IncomingQuantity,
            decimal IncomingUnitCost);

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
                    "No tiene acceso para realizar esta acción.",
                    "ERP:INVALID_ACCESS");
            }

            var inventoryEntry = await _unitOfWork.InventoryEntries.Entities
                .Include(entry => entry.Branch)
                .Include(entry => entry.WorkArea)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(order => order.Supplier)
                .Include(entry => entry.PurchaseOrder)
                    .ThenInclude(order => order.PurchaseRequest)
                        .ThenInclude(purchaseRequest => purchaseRequest.CostCenter)
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
                            .ThenInclude(product => product.CostCenter)
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

            if (inventoryEntry.Branch is not null && inventoryEntry.Branch.CompanyId != request.CompanyId)
            {
                return _errorManager.ThrowNotFound<InventoryEntryDto>(
                    "La entrada de inventario no fue encontrada.",
                    "ERP:INVENTORY_ENTRY_NOT_FOUND");
            }

            var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

            if (!isAdministrator && inventoryEntry.BranchId != access.Profile.BranchId)
            {
                return _errorManager.ThrowUnauthorized<InventoryEntryDto>(
                    "No tiene acceso para registrar entradas de inventario de otra sucursal.",
                    "ERP:INVALID_BRANCH_ACCESS");
            }

            if (inventoryEntry.EntryStatus != EntryStatus.PendingReview)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La entrada de inventario ya fue procesada.",
                    "ERP:INVENTORY_ENTRY_ALREADY_PROCESSED");
            }

            var purchaseOrder = inventoryEntry.PurchaseOrder;
            var purchaseRequest = purchaseOrder?.PurchaseRequest;

            var activeOrderItems = (purchaseOrder?.PurchaseOrderItems ?? [])
                .Where(item => item.DeletedAt == null)
                .OrderBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .ToList();

            if (activeOrderItems.Count == 0)
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "La orden de compra asociada no tiene ítems activos para registrar la entrada.",
                    "ERP:INVENTORY_ENTRY_WITHOUT_ITEMS");
            }

            var orderItemsById = activeOrderItems.ToDictionary(item => item.Id);

            if (request.Items is { Count: > 0 } &&
                request.Items.Any(item => !orderItemsById.ContainsKey(item.PurchaseOrderItemId)))
            {
                return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                    "Uno o más ítems no pertenecen a la orden de compra de la entrada.",
                    "ERP:INVENTORY_ENTRY_ITEM_NOT_IN_PURCHASE_ORDER");
            }

            var acceptedQuotationsByRequestItemId = (purchaseRequest?.PurchaseRequestItems ?? [])
                .SelectMany(item => item.Quotations)
                .Where(quotation => purchaseOrder?.SupplierId == null || quotation.SupplierId == purchaseOrder.SupplierId)
                .GroupBy(quotation => quotation.PurchaseRequestItemId)
                .ToDictionary(group => group.Key, group => group.First());

            var resolvedLines = new List<ResolvedEntryLine>(activeOrderItems.Count);

            foreach (var orderItem in activeOrderItems)
            {
                var product = orderItem.Product;

                if (product is null || product.DeletedAt != null)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        "Uno de los ítems de la orden de compra no tiene un producto válido asociado.",
                        "ERP:PURCHASE_ORDER_ITEM_WITHOUT_PRODUCT");
                }

                if (orderItem.Quantity <= 0)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        $"La cantidad del producto '{product.ProductName ?? product.Code}' debe ser mayor a cero.",
                        "ERP:INVALID_ITEM_QUANTITY");
                }

                var resolvedUnitCost = orderItem.UnitPrice;
                if (resolvedUnitCost <= 0 &&
                    orderItem.PurchaseRequestItemId.HasValue &&
                    acceptedQuotationsByRequestItemId.TryGetValue(orderItem.PurchaseRequestItemId.Value, out var acceptedQuote))
                {
                    resolvedUnitCost = acceptedQuote.PriceUnit;
                }

                if (resolvedUnitCost < 0)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        $"El costo unitario del producto '{product.ProductName ?? product.Code}' no puede ser negativo.",
                        "ERP:INVALID_ITEM_UNIT_COST");
                }

                var designatedCostCenter = product.CostCenter ?? purchaseRequest?.CostCenter;

                if (designatedCostCenter is null || !designatedCostCenter.IsActive || designatedCostCenter.DeletedAt != null)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        $"El producto '{product.ProductName ?? product.Code}' no tiene un centro de costo activo asociado.",
                        "ERP:PRODUCT_WITHOUT_COST_CENTER");
                }

                resolvedLines.Add(new ResolvedEntryLine(
                    orderItem,
                    product,
                    designatedCostCenter,
                    orderItem.Quantity,
                    resolvedUnitCost));
            }

            var productIds = resolvedLines.Select(line => line.Product.Id).Distinct().ToList();
            var costCenterIds = resolvedLines.Select(line => line.CostCenter.Id).Distinct().ToList();

            var existingStocks = await _unitOfWork.ProductStocks.Entities
                .Where(stock =>
                    productIds.Contains(stock.ProductId) &&
                    costCenterIds.Contains(stock.CostCenterId) &&
                    stock.DeletedAt == null)
                .ToListAsync(cancellationToken);

            var stocksByKey = existingStocks
                .GroupBy(stock => (stock.ProductId, stock.CostCenterId))
                .ToDictionary(group => group.Key, group => group.First());

            // Pre-validar límites de precisión numérica antes de subir archivos a S3
            var simulatedBalances = stocksByKey.ToDictionary(
                pair => pair.Key,
                pair => (Quantity: pair.Value.AvailableQuantity, AverageCost: pair.Value.WeightedAverageCost));

            foreach (var line in resolvedLines)
            {
                var key = (line.Product.Id, line.CostCenter.Id);
                simulatedBalances.TryGetValue(key, out var current);

                var previewCost = weightedAverageCostService.Calculate(
                    current.Quantity,
                    current.AverageCost,
                    line.IncomingQuantity,
                    line.IncomingUnitCost);

                if (previewCost.NewQuantity > MaxStockQuantityPrecision)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        $"La cantidad resultante en stock del producto '{line.Product.ProductName ?? line.Product.Code}' excede el límite permitido ({MaxStockQuantityPrecision:N2}).",
                        "ERP:STOCK_QUANTITY_OVERFLOW");
                }

                if (previewCost.NewAverageCost > MaxStockAverageCostPrecision)
                {
                    return _errorManager.ThrowBadRequest<InventoryEntryDto>(
                        $"El costo promedio ponderado resultante del producto '{line.Product.ProductName ?? line.Product.Code}' excede el límite permitido ({MaxStockAverageCostPrecision:N2}).",
                        "ERP:STOCK_AVERAGE_COST_OVERFLOW");
                }

                simulatedBalances[key] = (previewCost.NewQuantity, previewCost.NewAverageCost);
            }

            var evidenceImages = new List<EntryEvidenceImage>(request.EvidenceImagesBase64.Count);

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

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, NicaraguaTimeZone);
            var transactionDate = DateOnly.FromDateTime(localNow);
            var transactionTime = TimeOnly.FromDateTime(localNow);

            var supplierName = purchaseOrder?.Supplier?.SuppliersLegalName
                ?? purchaseOrder?.Supplier?.CommercialName;

            var additionalItems = new List<PurchaseOrderEntryAdditionalItem>(resolvedLines.Count);

            foreach (var line in resolvedLines)
            {
                var stockKey = (line.Product.Id, line.CostCenter.Id);
                stocksByKey.TryGetValue(stockKey, out var productStock);

                var previousQuantity = productStock?.AvailableQuantity ?? 0m;
                var previousAverageCost = productStock?.WeightedAverageCost ?? 0m;

                var weightedCost = weightedAverageCostService.Calculate(
                    previousQuantity,
                    previousAverageCost,
                    line.IncomingQuantity,
                    line.IncomingUnitCost);

                if (productStock is null)
                {
                    productStock = new ProductStock
                    {
                        Id = Guid.NewGuid(),
                        IsActive = true,
                        MinimumStock = 0m,
                        AvailableQuantity = weightedCost.NewQuantity,
                        WeightedAverageCost = weightedCost.NewAverageCost,
                        Status = ResolveStockStatus(weightedCost.NewQuantity, 0m),
                        UnitMeasureId = line.Product.UnitMeasureId,
                        ProductId = line.Product.Id,
                        CostCenterId = line.CostCenter.Id
                    };

                    await _unitOfWork.ProductStocks.RegisterProductStock(productStock);
                    stocksByKey[stockKey] = productStock;
                }
                else
                {
                    productStock.IsActive = true;
                    productStock.AvailableQuantity = weightedCost.NewQuantity;
                    productStock.WeightedAverageCost = weightedCost.NewAverageCost;
                    productStock.Status = ResolveStockStatus(productStock.AvailableQuantity, productStock.MinimumStock);
                }

                var workAreaId = line.CostCenter.WorkAreaId != Guid.Empty
                    ? line.CostCenter.WorkAreaId
                    : inventoryEntry.WorkAreaId;

                var kardex = KardexMapper.ToEntryKardexEntity(
                    line.Product,
                    line.IncomingQuantity,
                    line.IncomingUnitCost,
                    weightedCost,
                    request.UserId,
                    workAreaId,
                    line.CostCenter.Id,
                    transactionDate,
                    transactionTime);

                await _unitOfWork.Kardex.RegisterKardexMovement(kardex);

                var roundedUnitCost = kardex.UnitCost;

                Guid? quotationId = null;
                if (line.OrderItem.PurchaseRequestItemId.HasValue &&
                    acceptedQuotationsByRequestItemId.TryGetValue(line.OrderItem.PurchaseRequestItemId.Value, out var quotation))
                {
                    quotationId = quotation.Id;
                }

                additionalItems.Add(new PurchaseOrderEntryAdditionalItem
                {
                    PurchaseOrderItemId = line.OrderItem.Id,
                    PurchaseRequestItemId = line.OrderItem.PurchaseRequestItemId,
                    QuotationId = quotationId,
                    ProductId = line.Product.Id,
                    ProductName = line.Product.ProductName,
                    ProductCode = line.Product.Code,
                    Quantity = line.OrderItem.Quantity,
                    UnitPrice = roundedUnitCost,
                    UnitMeasureId = line.Product.UnitMeasureId,
                    UnitMeasureName = line.Product.UnitMeasure?.Name,
                    UnitMeasureSymbol = line.Product.UnitMeasure?.Symbol,
                    SupplierId = purchaseOrder?.SupplierId,
                    SupplierName = supplierName,
                    CostCenterId = line.CostCenter.Id,
                    CostCenterName = line.CostCenter.CostCenterName,
                    CostCenterCode = line.CostCenter.CostCenterCode,
                    CostCenterDescription = line.CostCenter.Description,
                    CostCenterCoilCode = line.CostCenter.CoilCode,
                });
            }

            var additionalData = DeserializeAdditionalData(inventoryEntry.AdditionalData);
            additionalData.PurchaseOrderId = purchaseOrder?.Id ?? inventoryEntry.PurchaseOrderId;
            additionalData.SupplierId = purchaseOrder?.SupplierId ?? additionalData.SupplierId;
            additionalData.SupplierName = supplierName ?? additionalData.SupplierName;
            additionalData.EvidenceImages.Clear();
            additionalData.EvidenceImages.AddRange(evidenceImages);
            additionalData.Items = additionalItems;

            inventoryEntry.Observations = string.IsNullOrWhiteSpace(request.Observations)
                ? inventoryEntry.Observations
                : request.Observations.Trim();
            inventoryEntry.InvoiceNumber = request.InvoiceNumber.Trim();
            inventoryEntry.InvoiceDate = request.InvoiceDate;
            inventoryEntry.AdditionalData = JsonSerializer.Serialize(additionalData, JsonManager.SnakeCaseOptions);
            inventoryEntry.EntryStatus = EntryStatus.ApprovedEntry;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<InventoryEntryDto>(inventoryEntry);
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

        private static ProductStockStatus ResolveStockStatus(decimal availableQuantity, decimal minimumStock)
        {
            if (availableQuantity <= 0m)
            {
                return ProductStockStatus.Empty;
            }

            return availableQuantity <= minimumStock
                ? ProductStockStatus.LowBalance
                : ProductStockStatus.Stable;
        }
    }
}
