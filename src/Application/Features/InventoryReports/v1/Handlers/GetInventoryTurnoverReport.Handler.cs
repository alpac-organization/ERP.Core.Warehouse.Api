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

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;
        var isManager = access.Role?.RoleType == RoleType.Manager;

        var stockQuery = _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.Product.ProductUsageType == ProductUsageType.Operational);

        if (isAdministrator)
        {
            if (request.BranchId.HasValue) stockQuery = stockQuery.Where(ps => ps.Product.BranchId == request.BranchId.Value);
            if (request.CostCenterId.HasValue) stockQuery = stockQuery.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        }
        else if (isManager)
        {
            stockQuery = stockQuery.Where(ps => ps.Product.BranchId == access.Profile.BranchId);
            if (request.CostCenterId.HasValue) stockQuery = stockQuery.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        }
        else
        {
            stockQuery = stockQuery.Where(ps => ps.Product.BranchId == access.Profile.BranchId)
                                 .Where(ps => ps.CostCenterId == access.Profile.CostCenterId)
                                 .Where(ps => ps.Product.CostCenter.WorkAreaId == access.Profile.AreaId);
        }

        if (request.BranchId.HasValue && !isAdministrator && !isManager)
            stockQuery = stockQuery.Where(ps => ps.Product.BranchId == request.BranchId.Value);
        if (request.CostCenterId.HasValue)
            stockQuery = stockQuery.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        if (request.ProductId.HasValue)
            stockQuery = stockQuery.Where(ps => ps.ProductId == request.ProductId.Value);
        if (!string.IsNullOrEmpty(request.ProductCode))
            stockQuery = stockQuery.Where(ps => ps.Product.Code == request.ProductCode);

        var stocks = await stockQuery
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
            .Include(ps => ps.CostCenter)
            .ToListAsync(ct);

        if (stocks.Count == 0) return [];

        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();

        var toDate = request.To ?? DateTime.UtcNow;
        var fromDate = request.From ?? toDate.AddMonths(-1);
        var yearStart = new DateTime(toDate.Year, 1, 1);

        // Fetch kardex for these products up to toDate (all history <= toDate)
        var kardexQuery = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Where(k => productIds.Contains(k.ProductId))
            .Where(k => k.CreatedAt <= toDate);

        if (request.CostCenterId.HasValue)
            kardexQuery = kardexQuery.Where(k => k.CostCenterId == request.CostCenterId.Value);
        if (request.BranchId.HasValue)
            kardexQuery = kardexQuery.Where(k => k.CostCenter.BranchId == request.BranchId.Value);

        var kardices = await kardexQuery.ToListAsync(ct);

        var result = mapper.Map<List<InventoryTurnoverReportDto>>(stocks);

        for (var i = 0; i < stocks.Count; i++)
        {
            var ps = stocks[i];
            var dto = result[i];

            var productKardex = kardices.Where(k => k.ProductId == ps.ProductId).ToList();

            var periodBuys = productKardex.Where(k => k.MovementType == KardexMovementType.Buys
                && k.CreatedAt >= fromDate && k.CreatedAt <= toDate).ToList();
            var periodExits = productKardex.Where(k => k.MovementType == KardexMovementType.Exit
                && k.CreatedAt >= fromDate && k.CreatedAt <= toDate).ToList();
            var annualExits = productKardex.Where(k => k.MovementType == KardexMovementType.Exit
                && k.CreatedAt >= yearStart && k.CreatedAt <= toDate).ToList();

            var historicalCosts = productKardex.Select(k => k.UnitCost).ToList();

            dto.TotalPurchases = periodBuys.Sum(k => k.Units);
            dto.LastPurchaseDate = periodBuys.Count > 0 ? periodBuys.Max(k => k.CreatedAt) : null;
            dto.TotalDispatches = periodExits.Sum(k => k.Units);
            dto.PurchaseUnits = periodBuys.Sum(k => k.Units);
            dto.LastDispatchDate = periodExits.Count > 0 ? periodExits.Max(k => k.CreatedAt) : null;
            dto.DispatchedUnits = periodExits.Sum(k => k.Units);
            dto.HistoricalMinCost = historicalCosts.Count > 0 ? historicalCosts.Min() : 0;
            dto.HistoricalMaxCost = historicalCosts.Count > 0 ? historicalCosts.Max() : 0;
            dto.MinDispatchedUnits = periodExits.Count > 0 ? periodExits.Min(k => k.Units) : 0;
            dto.MaxDispatchedUnits = periodExits.Count > 0 ? periodExits.Max(k => k.Units) : 0;
            dto.MonthlyRotationIndex = periodExits.Sum(k => k.Units);
            dto.AnnualRotationIndex = annualExits.Sum(k => k.Units);
        }

        return result.OrderBy(r => r.Category).ThenBy(r => r.ProductName).ToList();
    }
}