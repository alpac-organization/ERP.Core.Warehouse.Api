using AutoMapper;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Pdf;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

using KardexEntity = ERP.Core.Database.Domain.Entities.Inventory.Kardex;
using CompanyEntity = ERP.Core.Database.Domain.Entities.Catalogs.Company;
using ProductEntity = ERP.Core.Database.Domain.Entities.Warehouse.Product;
using ProductStockEntity = ERP.Core.Database.Domain.Entities.Inventory.ProductStock;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

public class GetKardexReportHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IQuestPdfGeneratorServices pdfGenerator)
    : InventoryReportHandlerBase<GetKardexReportQuery, ReportPdfFileDto>(unitOfWork, errorManager)
{
    private sealed record InitialBalance(decimal Units, decimal Kardex);

    private sealed record ReportScope(
        Guid BranchId,
        DateOnly From,
        DateOnly To,
        string? GeneratedBy,
        CompanyEntity Company);

    public override async Task<ReportPdfFileDto> Handle(GetKardexReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var error = ValidateRequest(request, access);
        if (error is not null)
            return error;

        var branchId = ResolveBranchId(request, access);

        error = await ValidateBranchCompanyAsync(request, access, branchId, ct);
        if (error is not null)
            return error;

        var scope = await CreateScopeAsync(request, access, branchId, ct);

        var productStockIds = NormalizeProductStockIds(request.ProductStockIds);
        if (productStockIds.Count == 0)
            return ProductRequiredError();

        var stocks = await LoadStocksAsync(branchId, productStockIds, ct);
        if (stocks.Count == 0)
            return await BuildPdfAsync(request, [], scope, ct);

        var productSources = await BuildProductSourcesAsync(request, scope, stocks, ct);
        var products = mapper.Map<List<KardexProductReportDto>>(productSources);

        return await BuildPdfAsync(request, products, scope, ct);
    }

    private ReportPdfFileDto? ValidateRequest(
        GetKardexReportQuery request,
        AccessValidationResult<ReportPdfFileDto> access)
    {
        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>(
                "Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>("El periodo no es valido.", "ERP:010");

        if (request.ProductStockIds is not { Count: > 0 })
            return ProductRequiredError();

        if (access.Role?.RoleType != RoleType.Administrator && access.Profile == null)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>("Perfil no encontrado.", "ERP:011");

        if (access.Role?.RoleType == RoleType.Administrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>("Debe especificar la sucursal.", "ERP:008");

        return null;
    }

    private static Guid ResolveBranchId(
        GetKardexReportQuery request,
        AccessValidationResult<ReportPdfFileDto> access)
        => access.Role?.RoleType == RoleType.Administrator
            ? request.BranchId!.Value
            : access.Profile!.BranchId;

    private async Task<ReportPdfFileDto?> ValidateBranchCompanyAsync(
        GetKardexReportQuery request,
        AccessValidationResult<ReportPdfFileDto> access,
        Guid branchId,
        CancellationToken ct)
    {
        if (access.Role?.RoleType != RoleType.Administrator)
            return null;

        var belongsToCompany = await _unitOfWork.Branches.Entities
            .AsNoTracking()
            .AnyAsync(b => b.Id == branchId && b.CompanyId == request.CompanyId && b.DeletedAt == null, ct);

        return belongsToCompany
            ? null
            : _errorManager.ThrowBadRequest<ReportPdfFileDto>(
                "La sucursal seleccionada no pertenece a la empresa actual.", "ERP:013");
    }

    private async Task<ReportScope> CreateScopeAsync(
        GetKardexReportQuery request,
        AccessValidationResult<ReportPdfFileDto> access,
        Guid branchId,
        CancellationToken ct)
    {
        var generatedBy = access.User?.Fullname ?? await GetGeneratedByAsync(request.UserId, ct);

        return new ReportScope(
            branchId,
            DateOnly.FromDateTime(request.From!.Value),
            DateOnly.FromDateTime(request.To!.Value),
            generatedBy,
            access.Company);
    }

    private static List<Guid> NormalizeProductStockIds(List<Guid>? productStockIds) =>
        productStockIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

    private ReportPdfFileDto ProductRequiredError() =>
        _errorManager.ThrowBadRequest<ReportPdfFileDto>(
            "Debe enviar al menos un producto de inventario.", "ERP:PRODUCT_REQUIRED");

    private async Task<List<ProductStockEntity>> LoadStocksAsync(
        Guid branchId,
        List<Guid> productStockIds,
        CancellationToken ct) =>
        await _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.DeletedAt == null &&
                         ps.Product.BranchId == branchId &&
                         productStockIds.Contains(ps.Id))
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
            .Include(ps => ps.Product)
                .ThenInclude(p => p.UnitMeasure)
            .OrderBy(ps => ps.Product.Code)
            .ThenBy(ps => ps.Product.ProductName)
            .ToListAsync(ct);

    private async Task<List<KardexProductSource>> BuildProductSourcesAsync(
        GetKardexReportQuery request,
        ReportScope scope,
        List<ProductStockEntity> stocks,
        CancellationToken ct)
    {
        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var costCenterIds = stocks.Select(s => s.CostCenterId).Distinct().ToList();

        var periodMovements = await LoadMovementsAsync(
            request, scope.BranchId, productIds, costCenterIds, scope.From, scope.To, false, ct);
        var initialMovements = await LoadMovementsAsync(
            request, scope.BranchId, productIds, costCenterIds, scope.From, scope.To, true, ct);

        var initialBalances = BuildInitialBalances(initialMovements);
        var movementsByProduct = GroupMovementsByProduct(periodMovements);

        return [.. GetOrderedProducts(stocks).Select(product => BuildProductSource(product, initialBalances, movementsByProduct))];
    }

    private static Dictionary<Guid, InitialBalance> BuildInitialBalances(List<KardexEntity> movements) =>
        movements
            .GroupBy(k => new { k.ProductId, k.CostCenterId })
            .Select(g => g.First())
            .GroupBy(k => k.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new InitialBalance(
                    Round(g.Sum(k => k.TotalUnits)),
                    Round(g.Sum(k => k.TotalKardex))));

    private static Dictionary<Guid, List<KardexEntity>> GroupMovementsByProduct(List<KardexEntity> movements) =>
        movements
            .GroupBy(k => k.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

    private static List<ProductEntity> GetOrderedProducts(List<ProductStockEntity> stocks) =>
        [.. stocks
            .GroupBy(s => s.ProductId)
            .Select(g => g.First().Product)];

    private static KardexProductSource BuildProductSource(
        ProductEntity product,
        Dictionary<Guid, InitialBalance> initialBalances,
        Dictionary<Guid, List<KardexEntity>> movementsByProduct)
    {
        initialBalances.TryGetValue(product.Id, out var initial);

        var initialUnits = initial?.Units ?? 0m;
        var initialKardex = initial?.Kardex ?? 0m;
        var movements = movementsByProduct.TryGetValue(product.Id, out var productMovements)
            ? productMovements
            : [];
        var rows = BuildRowSources(movements, initialUnits, initialKardex);

        return new KardexProductSource
        {
            Product = product,
            InitialKardexBalance = initialKardex,
            InitialInventory = initialUnits,
            InitialAverageCost = initialUnits > 0m ? Round(initialKardex / initialUnits) : 0m,
            Rows = rows,
            Summary = BuildSummary(initialUnits, initialKardex, rows)
        };
    }

    private static List<KardexMovementSource> BuildRowSources(
        List<KardexEntity> movements,
        decimal initialUnits,
        decimal initialKardex)
    {
        var rows = new List<KardexMovementSource>();
        var runningUnits = initialUnits;
        var runningKardex = initialKardex;

        foreach (var movement in movements)
        {
            var isEntry = movement.MovementType == KardexMovementType.Buys;
            var units = Round(movement.Units);
            var totalInput = Round(movement.TotalInput);
            var totalOutput = Round(movement.TotalOutput);

            if (isEntry)
            {
                runningUnits += units;
                runningKardex += totalInput;
            }
            else
            {
                runningUnits -= units;
                runningKardex -= totalOutput;
            }

            runningUnits = Round(runningUnits);
            runningKardex = Round(runningKardex);

            rows.Add(new KardexMovementSource
            {
                Kardex = movement,
                MovementType = isEntry ? "Entrada" : "Salida",
                RecordNumber = isEntry
                    ? movement.InventoryEntry?.Code
                    : movement.InventoryOutput?.Code,
                Document = ResolveReferenceType(movement),
                BalanceUnits = runningUnits,
                AverageCost = runningUnits > 0m ? Round(runningKardex / runningUnits) : 0m,
                KardexBalance = runningKardex
            });
        }

        return rows;
    }

    private static KardexProductSummaryDto BuildSummary(
        decimal initialUnits,
        decimal initialKardex,
        List<KardexMovementSource> rows)
    {
        var totalEntries = Round(rows
            .Where(s => s.Kardex.MovementType == KardexMovementType.Buys)
            .Sum(s => s.Kardex.Units));
        var totalExits = Round(rows
            .Where(s => s.Kardex.MovementType == KardexMovementType.Exit)
            .Sum(s => s.Kardex.Units));
        var totalEntryCost = Round(rows.Sum(s => s.Kardex.TotalInput));
        var totalExitCost = Round(rows.Sum(s => s.Kardex.TotalOutput));
        var finalUnits = Round(initialUnits + totalEntries - totalExits);
        var finalKardexBalance = Round(initialKardex + totalEntryCost - totalExitCost);

        return new KardexProductSummaryDto
        {
            RowCount = rows.Count,
            TotalEntries = totalEntries,
            TotalExits = totalExits,
            TotalEntryCost = totalEntryCost,
            TotalExitCost = totalExitCost,
            FinalUnits = finalUnits,
            FinalAverageCost = finalUnits > 0m ? Round(finalKardexBalance / finalUnits) : 0m,
            FinalKardexBalance = finalKardexBalance
        };
    }

    private async Task<List<KardexEntity>> LoadMovementsAsync(
        GetKardexReportQuery request,
        Guid branchId,
        List<Guid> productIds,
        List<Guid> costCenterIds,
        DateOnly from,
        DateOnly to,
        bool beforePeriod,
        CancellationToken ct)
    {
        var query = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Include(k => k.InventoryEntry)
            .Include(k => k.InventoryOutput)
            .Where(k => k.DeletedAt == null)
            .Where(k => k.MovementType == KardexMovementType.Buys || k.MovementType == KardexMovementType.Exit)
            .Where(k => productIds.Contains(k.ProductId))
            .Where(k => costCenterIds.Contains(k.CostCenterId))
            .Where(k =>
                (k.MovementType == KardexMovementType.Buys && k.CostCenter.BranchId == branchId) ||
                (k.MovementType == KardexMovementType.Exit &&
                 k.InventoryOutput != null &&
                 k.InventoryOutput.BranchId == branchId))
            .Where(k =>
                k.MovementType != KardexMovementType.Exit ||
                (k.InventoryOutput != null &&
                 k.InventoryOutput.Status == InventoryOutputsStatus.Dispatched &&
                 k.InventoryOutput.DeletedAt == null));

        if (request.CostCenterId.HasValue)
            query = query.Where(k => k.CostCenterId == request.CostCenterId.Value);

        if (beforePeriod)
        {
            query = query.Where(k => k.TransactionDate < from);
            return await query
                .OrderByDescending(k => k.TransactionDate)
                .ThenByDescending(k => k.TransactionTime)
                .ThenByDescending(k => k.CreatedAt)
                .ToListAsync(ct);
        }

        query = query.Where(k => k.TransactionDate >= from && k.TransactionDate <= to);
        return await query
            .OrderBy(k => k.TransactionDate)
            .ThenBy(k => k.TransactionTime)
            .ThenBy(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    private static ReferenceType ResolveReferenceType(KardexEntity movement)
    {
        if (movement.ReferenceType is ReferenceType.FT or ReferenceType.BL or ReferenceType.RQ)
            return movement.ReferenceType;

        if (movement.InventoryOutput?.FuelRequestId.HasValue == true)
            return ReferenceType.BL;

        if (movement.InventoryOutput?.SupplyRequestId.HasValue == true)
            return ReferenceType.RQ;

        if (movement.InventoryEntryId.HasValue || movement.MovementType == KardexMovementType.Buys)
            return ReferenceType.FT;

        return ReferenceType.None;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private async Task<ReportPdfFileDto> BuildPdfAsync(
        GetKardexReportQuery request,
        List<KardexProductReportDto> products,
        ReportScope scope,
        CancellationToken ct)
    {
        var report = new KardexReportDto
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = scope.GeneratedBy,
            From = request.From!.Value,
            To = request.To!.Value,
            Products = products
        };

        var generatedBy = string.IsNullOrWhiteSpace(scope.GeneratedBy)
            ? "Sistema"
            : scope.GeneratedBy.Trim();

        var branding = await CompanyBrandingResolver.ResolveAsync(scope.Company, ct);
        var logoImage = branding.Logo is { Length: > 0 }
            ? Image.FromBinaryData(branding.Logo)
            : null;

        var bytes = pdfGenerator.Generate(
            KardexPdfComposer.Compose(report, generatedBy, report.GeneratedAt, logoImage));

        var fileName = $"reporte-kardex-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.pdf";

        return new ReportPdfFileDto(bytes, fileName);
    }
}
