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
/// Genera el reporte de Inventario de Periodo basado en Kardex: cantidades y costos iniciales, entradas, salidas y finales.
/// </summary>
public class GetPeriodInventoryReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetPeriodInventoryReportQuery, List<PeriodInventoryReportDto>>(unitOfWork, errorManager)
{
    public override async Task<List<PeriodInventoryReportDto>> Handle(GetPeriodInventoryReportQuery request, CancellationToken ct)
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

        var stocks = await stockQuery
            .Include(ps => ps.Product)
            .ToListAsync(ct);

        if (stocks.Count == 0) return [];

        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();

        var kardexQuery = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Where(k => productIds.Contains(k.ProductId))
            .Where(k => k.CreatedAt <= request.To);

        if (request.CostCenterId.HasValue)
            kardexQuery = kardexQuery.Where(k => k.CostCenterId == request.CostCenterId.Value);
        if (request.BranchId.HasValue)
            kardexQuery = kardexQuery.Where(k => k.CostCenter.BranchId == request.BranchId.Value);

        var kardices = await kardexQuery.ToListAsync(ct);

        var result = mapper.Map<List<PeriodInventoryReportDto>>(stocks);

        for (var i = 0; i < stocks.Count; i++)
        {
            var ps = stocks[i];
            var dto = result[i];

            var productKardex = kardices
                .Where(k => k.ProductId == ps.ProductId)
                .OrderBy(k => k.CreatedAt)
                .ToList();

            var beforeFrom = productKardex.Where(k => k.CreatedAt < request.From).ToList();
            var period = productKardex.Where(k => k.CreatedAt >= request.From && k.CreatedAt <= request.To).ToList();

            var initialMovement = beforeFrom.LastOrDefault();
            var initialQty = initialMovement?.TotalUnits ?? 0;
            var initialCost = initialMovement?.AverageCost ?? 0;

            var entryQty = period.Where(k => k.MovementType == KardexMovementType.Buys).Sum(k => k.Units);
            var entryCost = period.Where(k => k.MovementType == KardexMovementType.Buys).Sum(k => k.TotalInput);
            var exitQty = period.Where(k => k.MovementType == KardexMovementType.Exit).Sum(k => k.Units);
            var exitCost = period.Where(k => k.MovementType == KardexMovementType.Exit).Sum(k => k.TotalOutput);

            var lastMovement = productKardex.Where(k => k.CreatedAt <= request.To).LastOrDefault();
            var finalQty = lastMovement?.TotalUnits ?? (initialQty + entryQty - exitQty);
            var finalCost = lastMovement?.AverageCost ?? (finalQty > 0 ? (initialQty * initialCost + entryCost - exitCost) / finalQty : 0);

            dto.InitialQuantity = initialQty;
            dto.InitialCost = initialCost;
            dto.InitialTotal = initialQty * initialCost;
            dto.EntryQuantity = entryQty;
            dto.EntryTotalCost = entryCost;
            dto.ExitQuantity = exitQty;
            dto.ExitTotalCost = exitCost;
            dto.FinalQuantity = finalQty;
            dto.FinalCost = finalCost;
            dto.FinalTotal = finalQty * finalCost;
        }

        return result.OrderBy(r => r.ProductName).ToList();
    }
}