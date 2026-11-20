using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Movimiento de Inventario: compras y salidas en el periodo, con costos historicos minimo/maximo.
/// </summary>
public class GetInventoryMovementReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetInventoryMovementReportQuery, InventoryMovementReportDto>(unitOfWork, errorManager)
{
    public override async Task<InventoryMovementReportDto> Handle(GetInventoryMovementReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var result = new InventoryMovementReportDto();

        // ---------- COMPRAS ----------
        var purchaseQuery = _unitOfWork.PurchaseOrders.Entities
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.PaymentRequest)
            .Include(po => po.PurchaseRequest)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Product)
            .Where(po => po.PurchaseRequest != null);

        if (request.BranchId.HasValue)
            purchaseQuery = purchaseQuery.Where(po => po.PurchaseRequest!.BranchId == request.BranchId.Value);
        if (request.CostCenterId.HasValue)
            purchaseQuery = purchaseQuery.Where(po => po.PurchaseRequest!.CostCenterId == request.CostCenterId.Value);
        if (request.ProductId.HasValue)
            purchaseQuery = purchaseQuery.Where(po => po.PurchaseOrderItems.Any(poi => poi.ProductId == request.ProductId.Value));
        if (!string.IsNullOrEmpty(request.ProductCode))
            purchaseQuery = purchaseQuery.Where(po => po.PurchaseOrderItems.Any(poi => poi.Product != null && poi.Product.Code == request.ProductCode));

        if (request.From.HasValue)
            purchaseQuery = purchaseQuery.Where(po => po.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            purchaseQuery = purchaseQuery.Where(po => po.CreatedAt <= request.To.Value);

        var purchaseOrders = await purchaseQuery.ToListAsync(ct);

        var purchaseItems = purchaseOrders
            .SelectMany(po => po.PurchaseOrderItems)
            .Where(item => !request.ProductId.HasValue || item.ProductId == request.ProductId.Value)
            .Where(item => string.IsNullOrEmpty(request.ProductCode) || item.Product?.Code == request.ProductCode)
            .ToList();

        result.Purchases = mapper.Map<List<InventoryMovementPurchaseDto>>(purchaseItems);

        // Costos historicos de compra (sin filtro de fecha)
        var historicalPurchaseQuery = _unitOfWork.PurchaseOrders.Entities
            .AsNoTracking()
            .Include(po => po.PurchaseRequest)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(poi => poi.Product)
            .Where(po => po.PurchaseRequest != null);

        if (request.BranchId.HasValue)
            historicalPurchaseQuery = historicalPurchaseQuery.Where(po => po.PurchaseRequest!.BranchId == request.BranchId.Value);
        if (request.CostCenterId.HasValue)
            historicalPurchaseQuery = historicalPurchaseQuery.Where(po => po.PurchaseRequest!.CostCenterId == request.CostCenterId.Value);
        if (request.ProductId.HasValue)
            historicalPurchaseQuery = historicalPurchaseQuery.Where(po => po.PurchaseOrderItems.Any(poi => poi.ProductId == request.ProductId.Value));
        if (!string.IsNullOrEmpty(request.ProductCode))
            historicalPurchaseQuery = historicalPurchaseQuery.Where(po => po.PurchaseOrderItems.Any(poi => poi.Product != null && poi.Product.Code == request.ProductCode));

        var historicalOrders = await historicalPurchaseQuery.ToListAsync(ct);
        var historicalCosts = historicalOrders
            .SelectMany(po => po.PurchaseOrderItems)
            .Where(i => request.ProductId == null || i.ProductId == request.ProductId.Value)
            .Where(i => string.IsNullOrEmpty(request.ProductCode) || true)
            .Select(i => i.UnitPrice)
            .ToList();

        if (historicalCosts.Count > 0)
        {
            result.PurchaseMinCost = historicalCosts.Min();
            result.PurchaseMaxCost = historicalCosts.Max();
        }

        // ---------- SALIDAS ----------
        var exitQuery = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Include(k => k.Product)
            .Include(k => k.InventoryOutput)
                .ThenInclude(io => io!.SupplyRequest)
                    .ThenInclude(sr => sr!.OperationalOrder)
            .Include(k => k.InventoryOutput)
                .ThenInclude(io => io!.FuelRequest)
            .Where(k => k.MovementType == KardexMovementType.Exit);

        if (request.From.HasValue)
            exitQuery = exitQuery.Where(k => k.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            exitQuery = exitQuery.Where(k => k.CreatedAt <= request.To.Value);
        if (request.ProductId.HasValue)
            exitQuery = exitQuery.Where(k => k.ProductId == request.ProductId.Value);
        if (!string.IsNullOrEmpty(request.ProductCode))
            exitQuery = exitQuery.Where(k => k.Product != null && k.Product.Code == request.ProductCode);
        if (request.CostCenterId.HasValue)
            exitQuery = exitQuery.Where(k => k.CostCenterId == request.CostCenterId.Value);
        if (request.BranchId.HasValue)
            exitQuery = exitQuery.Where(k => k.CostCenter.BranchId == request.BranchId.Value);

        var exits = await exitQuery
            .OrderBy(k => k.CreatedAt)
            .ToListAsync(ct);

        result.Exits = mapper.Map<List<InventoryMovementExitDto>>(exits);

        if (result.Exits.Count > 0)
            result.ExitMaxCost = result.Exits.Max(e => e.UnitCost);

        // Ordenamiento: compras luego salidas (ya separados)
        result.Purchases = result.Purchases.OrderBy(p => p.Date).ToList();
        result.Exits = result.Exits.OrderBy(e => e.Date).ToList();

        return result;
    }
}