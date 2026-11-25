using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Inventario por Periodo: saldo inicial, entradas, salidas y saldo final por stock,
/// usando los movimientos historicos del Kardex.
/// </summary>
public class GetPeriodInventoryReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : InventoryReportHandlerBase<GetPeriodInventoryReportQuery, PeriodInventoryReportDto>(unitOfWork, errorManager)
{
    private sealed record StockKey(Guid ProductId, Guid CostCenterId);

    public override async Task<PeriodInventoryReportDto> Handle(GetPeriodInventoryReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<PeriodInventoryReportDto>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<PeriodInventoryReportDto>("El periodo no es valido.", "ERP:010");

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<PeriodInventoryReportDto>("Debe especificar una sucursal para consultar el inventario del periodo.", "ERP:007");

        var branchId = isAdministrator ? request.BranchId!.Value : access.Profile.BranchId;
        var from = DateOnly.FromDateTime(request.From!.Value);
        var to = DateOnly.FromDateTime(request.To!.Value);
        var generatedBy = await GetGeneratedByAsync(request.UserId, ct);

        var stocks = await LoadStocksAsync(branchId, request.CostCenterId, ct);
        if (stocks.Count == 0)
            return BuildReport([], request, generatedBy);

        var keys = stocks.Select(s => new StockKey(s.ProductId, s.CostCenterId)).ToHashSet();
        var productIds = keys.Select(k => k.ProductId).Distinct().ToList();
        var costCenterIds = keys.Select(k => k.CostCenterId).Distinct().ToList();

        var movements = await _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Where(k => k.DeletedAt == null)
            .Where(k => k.TransactionDate <= to)
            .Where(k => productIds.Contains(k.ProductId))
            .Where(k => costCenterIds.Contains(k.CostCenterId))
            .OrderBy(k => k.TransactionDate)
            .ThenBy(k => k.TransactionTime)
            .ThenBy(k => k.CreatedAt)
            .ToListAsync(ct);

        var historyByKey = movements
            .Where(k => keys.Contains(new StockKey(k.ProductId, k.CostCenterId)))
            .GroupBy(k => new StockKey(k.ProductId, k.CostCenterId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = mapper.Map<List<PeriodInventoryItemDto>>(stocks);

        foreach (var (stock, item) in stocks.Zip(items))
        {
            var key = new StockKey(stock.ProductId, stock.CostCenterId);
            var history = historyByKey.TryGetValue(key, out var stockMovements)
                ? stockMovements
                : [];

            var initial = history.LastOrDefault(k => k.TransactionDate < from);
            var final = history.LastOrDefault();
            var period = history.Where(k => k.TransactionDate >= from && k.TransactionDate <= to).ToList();

            var entries = period.Where(k => k.MovementType == KardexMovementType.Buys).ToList();
            var exits = period.Where(k => k.MovementType == KardexMovementType.Exit).ToList();

            item.InitialQuantity = initial?.TotalUnits ?? 0m;
            item.InitialCost = initial?.AverageCost ?? 0m;
            item.InitialTotal = item.InitialQuantity * item.InitialCost;

            item.EntryQuantity = entries.Sum(k => k.Units);
            item.EntryTotalCost = entries.Sum(k => k.TotalInput);

            item.ExitQuantity = exits.Sum(k => k.Units);
            item.ExitTotalCost = exits.Sum(k => k.TotalOutput);

            item.FinalQuantity = final?.TotalUnits ?? 0m;
            item.FinalCost = final?.AverageCost ?? 0m;
            item.FinalTotal = item.FinalQuantity * item.FinalCost;
        }

        var orderedItems = items
            .OrderBy(i => i.ProductName)
            .ThenBy(i => i.StockCode)
            .ToList();

        return BuildReport(orderedItems, request, generatedBy);
    }

    private async Task<List<ProductStock>> LoadStocksAsync(Guid branchId, Guid? costCenterId, CancellationToken ct)
    {
        var query = _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.Product.ProductUsageType == ProductUsageType.Operational)
            .Where(ps => ps.Product.BranchId == branchId);

        if (costCenterId.HasValue)
            query = query.Where(ps => ps.CostCenterId == costCenterId.Value);

        return await query
            .Include(ps => ps.Product)
            .Include(ps => ps.CostCenter)
            .ToListAsync(ct);
    }

    private static PeriodInventoryReportDto BuildReport(
        List<PeriodInventoryItemDto> items,
        GetPeriodInventoryReportQuery request,
        string? generatedBy) => new()
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From!.Value,
            To = request.To!.Value,
            Items = items,
            TotalInitialTotal = items.Sum(i => i.InitialTotal),
            TotalEntryTotalCost = items.Sum(i => i.EntryTotalCost),
            TotalExitTotalCost = items.Sum(i => i.ExitTotalCost),
            TotalFinalTotal = items.Sum(i => i.FinalTotal)
        };
}
