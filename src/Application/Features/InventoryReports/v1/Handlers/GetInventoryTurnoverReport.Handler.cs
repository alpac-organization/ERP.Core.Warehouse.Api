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
/// Genera el reporte de Rotacion de Inventario: consolidado por producto con compras, despachos, costos historicos e indices de rotacion.
/// </summary>
public class GetInventoryTurnoverReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetInventoryTurnoverReportQuery, List<InventoryTurnoverReportDto>>(unitOfWork, errorManager)
{
    public override async Task<List<InventoryTurnoverReportDto>> Handle(GetInventoryTurnoverReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<List<InventoryTurnoverReportDto>>("El periodo no es valido.", "ERP:010");

        var roleType = access.Role?.RoleType;
        var isAdministrator = roleType == RoleType.Administrator;
        var isManager = roleType == RoleType.Manager;

        var stockQuery = _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.DeletedAt == null)
            .Where(ps => ps.Product.Branch.CompanyId == request.CompanyId)
            .Where(ps => ps.Product.ProductUsageType == ProductUsageType.Operational);

        if (isAdministrator)
        {
            if (request.BranchId.HasValue)
                stockQuery = stockQuery.Where(ps => ps.Product.BranchId == request.BranchId.Value);

            if (request.CostCenterId.HasValue)
                stockQuery = stockQuery.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        }
        else if (isManager)
        {
            stockQuery = stockQuery
                .Where(ps => ps.Product.BranchId == access.Profile.BranchId)
                .Where(ps => ps.CostCenter.BranchId == access.Profile.BranchId && ps.CostCenter.IsOperational);

            if (request.CostCenterId.HasValue)
                stockQuery = stockQuery.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        }
        else
        {
            stockQuery = stockQuery
                .Where(ps => ps.Product.BranchId == access.Profile.BranchId)
                .Where(ps => ps.CostCenterId == access.Profile.CostCenterId);
        }

        if (request.ProductId.HasValue)
            stockQuery = stockQuery.Where(ps => ps.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            var trimmedCode = request.ProductCode.Trim();
            stockQuery = stockQuery.Where(ps => ps.Product.Code == trimmedCode);
        }

        var stocks = await stockQuery
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
            .Include(ps => ps.CostCenter)
            .ToListAsync(ct);

        if (stocks.Count == 0)
            return [];

        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var costCenterIds = stocks.Select(s => s.CostCenterId).Distinct().ToList();

        var toDateTime = request.To ?? DateTime.UtcNow;
        var fromDateTime = request.From ?? toDateTime.AddMonths(-1);

        var toDate = DateOnly.FromDateTime(toDateTime);
        var fromDate = DateOnly.FromDateTime(fromDateTime);
        var yearStart = new DateOnly(toDate.Year, 1, 1);

        var kardices = await _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Where(k => k.DeletedAt == null)
            .Where(k => productIds.Contains(k.ProductId) && costCenterIds.Contains(k.CostCenterId))
            .Where(k => k.TransactionDate <= toDate)
            .Where(k =>
                k.MovementType != KardexMovementType.Exit ||
                (k.InventoryOutput != null &&
                 k.InventoryOutput.Status == InventoryOutputsStatus.Dispatched &&
                 k.InventoryOutput.DeletedAt == null))
            .ToListAsync(ct);

        var kardexByStockKey = kardices
            .GroupBy(k => (k.ProductId, k.CostCenterId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = mapper.Map<List<InventoryTurnoverReportDto>>(stocks);

        for (var i = 0; i < stocks.Count; i++)
        {
            var ps = stocks[i];
            var dto = result[i];

            var productKardex = kardexByStockKey.TryGetValue((ps.ProductId, ps.CostCenterId), out var list)
                ? list
                : [];

            var periodBuys = productKardex
                .Where(k => k.MovementType == KardexMovementType.Buys && k.TransactionDate >= fromDate && k.TransactionDate <= toDate)
                .ToList();

            var periodExits = productKardex
                .Where(k => k.MovementType == KardexMovementType.Exit && k.TransactionDate >= fromDate && k.TransactionDate <= toDate)
                .ToList();

            var annualExits = productKardex
                .Where(k => k.MovementType == KardexMovementType.Exit && k.TransactionDate >= yearStart && k.TransactionDate <= toDate)
                .ToList();

            var historicalCosts = productKardex.Select(k => k.UnitCost).ToList();

            dto.TotalPurchases = periodBuys.Count;
            dto.PurchaseUnits = periodBuys.Sum(k => k.Units);
            dto.LastPurchaseDate = periodBuys.Count > 0
                ? periodBuys.Max(k => k.TransactionDate.ToDateTime(k.TransactionTime))
                : null;

            dto.TotalDispatches = periodExits.Count;
            dto.DispatchedUnits = periodExits.Sum(k => k.Units);
            dto.LastDispatchDate = periodExits.Count > 0
                ? periodExits.Max(k => k.TransactionDate.ToDateTime(k.TransactionTime))
                : null;

            dto.HistoricalMinCost = historicalCosts.Count > 0 ? historicalCosts.Min() : 0m;
            dto.HistoricalMaxCost = historicalCosts.Count > 0 ? historicalCosts.Max() : 0m;
            dto.MinDispatchedUnits = periodExits.Count > 0 ? periodExits.Min(k => k.Units) : 0m;
            dto.MaxDispatchedUnits = periodExits.Count > 0 ? periodExits.Max(k => k.Units) : 0m;
            dto.MonthlyRotationIndex = periodExits.Sum(k => k.Units);
            dto.AnnualRotationIndex = annualExits.Sum(k => k.Units);
        }

        return result.OrderBy(r => r.Category).ThenBy(r => r.ProductName).ToList();
    }
}
