using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Stock Diario agrupado por sucursal, area, centro de costo y tipo de uso, con productos ordenados por centro de costo, tipo de uso y codigo.
/// </summary>
public class GetStockDailyReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetStockDailyReportQuery, StockDailyReportDto>(unitOfWork, errorManager)
{
    private record StockRow(ProductStock Stock, StockDailyProductDto Product);

    public override async Task<StockDailyReportDto> Handle(GetStockDailyReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var stocks = await LoadStocksAsync(request, ct);
        var products = MapProducts(stocks);

        var rows = stocks.Zip(products, (s, p) => new StockRow(s, p)).ToList();

        var report = new StockDailyReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            Branches = BuildBranches(rows)
        };

        var generatedBy = await ResolveGeneratedByAsync(request.UserId, ct);
        report.GeneratedBy = generatedBy;
        report.GeneratedByUserName = generatedBy;

        ApplySummary(report, products);

        return report;
    }

    private async Task<string?> ResolveGeneratedByAsync(Guid userId, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user?.Fullname;
    }

    private async Task<List<ProductStock>> LoadStocksAsync(GetStockDailyReportQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.ProductStocks.Entities.AsNoTracking();

        if (request.AreaId.HasValue)
            query = query.Where(ps => ps.Product.CostCenter != null && ps.Product.CostCenter.WorkAreaId == request.AreaId.Value);

        if (request.AreaIds is { Count: > 0 })
            query = query.Where(ps => ps.Product.CostCenter != null && request.AreaIds.Contains(ps.Product.CostCenter.WorkAreaId));

        if (request.BranchId.HasValue)
            query = query.Where(ps => ps.Product.BranchId == request.BranchId.Value);

        if (request.BranchIds is { Count: > 0 })
            query = query.Where(ps => request.BranchIds.Contains(ps.Product.BranchId));

        if (request.CostCenterId.HasValue)
            query = query.Where(ps => ps.CostCenterId == request.CostCenterId.Value);

        if (request.CostCenterIds is { Count: > 0 })
            query = query.Where(ps => request.CostCenterIds.Contains(ps.CostCenterId));

        if (request.CategoryId.HasValue)
            query = query.Where(ps => ps.Product.CategoryId == request.CategoryId.Value);

        if (request.CategoryIds is { Count: > 0 })
            query = query.Where(ps => request.CategoryIds.Contains(ps.Product.CategoryId));

        if (request.UsageType.HasValue)
            query = query.Where(ps => ps.Product.ProductUsageType == request.UsageType.Value);

        if (request.UsageTypes is { Count: > 0 })
            query = query.Where(ps => request.UsageTypes.Contains(ps.Product.ProductUsageType));

        if (!string.IsNullOrEmpty(request.ProductCode))
            query = query.Where(ps => ps.Product.Code == request.ProductCode);

        if (!string.IsNullOrEmpty(request.ProductName))
            query = query.Where(ps => EF.Functions.Like(ps.Product.ProductName!, $"%{request.ProductName}%"));

        return await query
            .Include(ps => ps.Product)
            .Include(ps => ps.CostCenter)
                .ThenInclude(cc => cc.WorkArea)
            .Include(ps => ps.UnitMeasure)
            .Include(ps => ps.Product.Branch)
            .OrderBy(ps => ps.Product.Branch != null ? ps.Product.Branch.BranchName : "Sucursal no asignada")
            .ThenBy(ps => ps.Product.CostCenter != null && ps.Product.CostCenter.WorkArea != null ? ps.Product.CostCenter.WorkArea.WorkAreaName : "Área no asignada")
            .ThenBy(ps => ps.CostCenter != null ? ps.CostCenter.CostCenterName : "Centro de costo no asignado")
            .ThenBy(ps => ps.Product.ProductUsageType)
            .ThenBy(ps => ps.Product.Code)
            .ToListAsync(ct);
    }

    private List<StockDailyProductDto> MapProducts(List<ProductStock> stocks)
    {
        var products = mapper.Map<List<StockDailyProductDto>>(stocks);

        for (var i = 0; i < stocks.Count; i++)
        {
            products[i].StockStatus = ResolveStockStatus(stocks[i]);
        }

        return products;
    }

    private static string ResolveStockStatus(ProductStock stock)
    {
        var quantity = stock.AvailableQuantity;
        var minimum = stock.MinimumStock;
        var maximum = stock.MaximumStock;

        if (quantity <= 0)
            return "Sin stock";

        if (quantity < minimum)
            return "Bajo mínimo";

        if (maximum.HasValue && quantity > maximum.Value)
            return "Sobre máximo";

        return "Normal";
    }

    private static void ApplySummary(StockDailyReportDto report, List<StockDailyProductDto> products)
    {
        var total = products.Count;
        var outOfStock = products.Count(p => p.StockStatus == "Sin stock");
        var belowMinimum = products.Count(p => p.StockStatus == "Bajo mínimo");
        var normal = products.Count(p => p.StockStatus == "Normal");
        var overMaximum = products.Count(p => p.StockStatus == "Sobre máximo");

        report.TotalProducts = total;
        report.OutOfStockCount = outOfStock;
        report.BelowMinimumCount = belowMinimum;
        report.NormalCount = normal;
        report.OverMaximumCount = overMaximum;
    }

    private static List<StockDailyBranchDto> BuildBranches(List<StockRow> rows)
    {
        return [.. rows
            .GroupBy(x => x.Stock.Product.BranchId)
            .OrderBy(g => g.First().Stock.Product.Branch != null ? g.First().Stock.Product.Branch.BranchName : "Sucursal no asignada")
            .Select(branchGroup =>
            {
                var branch = branchGroup.First().Stock.Product.Branch;

                return new StockDailyBranchDto
                {
                    BranchId = branch != null ? branch.Id : Guid.Empty,
                    BranchName = branch != null ? branch.BranchName : "Sucursal no asignada",
                    Areas = BuildAreas(branchGroup.ToList())
                };
            })];
    }

    private static List<StockDailyAreaDto> BuildAreas(List<StockRow> rows)
    {
        return [.. rows
            .GroupBy(x => x.Stock.Product.CostCenter != null ? x.Stock.Product.CostCenter.WorkAreaId : Guid.Empty)
            .OrderBy(g => g.First().Stock.Product.CostCenter != null && g.First().Stock.Product.CostCenter.WorkArea != null
                ? g.First().Stock.Product.CostCenter.WorkArea.WorkAreaName
                : "Área no asignada")
            .Select(areaGroup =>
            {
                var area = areaGroup.First().Stock.Product.CostCenter != null
                    ? areaGroup.First().Stock.Product.CostCenter.WorkArea
                    : null;

                return new StockDailyAreaDto
                {
                    AreaId = area != null ? area.Id : Guid.Empty,
                    AreaName = area != null ? area.WorkAreaName : "Área no asignada",
                    CostCenters = BuildCostCenters(areaGroup.ToList())
                };
            })];
    }

    private static List<StockDailyCostCenterDto> BuildCostCenters(List<StockRow> rows)
    {
        return [.. rows
            .GroupBy(x => x.Stock.CostCenterId)
            .OrderBy(g => g.First().Stock.CostCenter != null ? g.First().Stock.CostCenter.CostCenterName : "Centro de costo no asignado")
            .Select(costCenterGroup =>
            {
                var costCenter = costCenterGroup.First().Stock.CostCenter;
                var products = costCenterGroup.Select(x => x.Product).ToList();
                var outOfStock = products.Count(p => p.StockStatus == "Sin stock");
                var belowMinimum = products.Count(p => p.StockStatus == "Bajo mínimo");
                var normal = products.Count(p => p.StockStatus == "Normal");
                var overMaximum = products.Count(p => p.StockStatus == "Sobre máximo");

                return new StockDailyCostCenterDto
                {
                    CostCenterId = costCenter != null ? costCenter.Id : Guid.Empty,
                    CostCenterName = costCenter != null ? costCenter.CostCenterName : "Centro de costo no asignado",
                    UsageTypes = BuildUsageTypes(costCenterGroup.ToList()),

                    ProductCount = products.Count,
                    OutOfStockCount = outOfStock,
                    BelowMinimumCount = belowMinimum,
                    NormalCount = normal,
                    OverMaximumCount = overMaximum,
                };
            })];
    }

    private static List<StockDailyUsageTypeDto> BuildUsageTypes(List<StockRow> rows)
    {
        return [.. rows
            .GroupBy(x => x.Stock.Product.ProductUsageType)
            .OrderBy(g => g.Key.ToString())
            .Select(usageGroup => new StockDailyUsageTypeDto
            {
                UsageType = usageGroup.Key.ToString(),
                Products = usageGroup.Select(x => x.Product).ToList()
            })];
    }
}