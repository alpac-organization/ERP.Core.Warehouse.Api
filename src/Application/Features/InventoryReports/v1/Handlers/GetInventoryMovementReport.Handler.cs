using System.Text.Json;

using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Movimiento de Inventario: entradas por compra y salidas del periodo,
/// con costos historicos minimo/maximo tomados del Kardex.
/// </summary>
public class GetInventoryMovementReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : InventoryReportHandlerBase<GetInventoryMovementReportQuery, InventoryMovementReportDto>(unitOfWork, errorManager)
{
    private sealed class EntryCandidate
    {
        public InventoryEntry Entry { get; init; } = default!;
        public string? RootSupplierName { get; init; }
        public List<PurchaseOrderEntryAdditionalItem> Items { get; init; } = [];
        public bool[] Used { get; set; } = [];
    }

    public override async Task<InventoryMovementReportDto> Handle(GetInventoryMovementReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("El periodo no es valido.", "ERP:010");

        var roleType = access.Role?.RoleType;
        var isAdministrator = roleType == RoleType.Administrator;
        var isManager = roleType == RoleType.Manager;

        if (!isAdministrator && access.Profile == null)
            return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("Perfil no encontrado.", "ERP:011");

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("Debe especificar una sucursal para consultar los movimientos.", "ERP:012");

        var branchId = isAdministrator ? request.BranchId!.Value : access.Profile!.BranchId;

        if (isAdministrator)
        {
            var branchBelongsToCompany = await _unitOfWork.Branches.Entities
                .AsNoTracking()
                .AnyAsync(b => b.Id == branchId && b.CompanyId == request.CompanyId && b.DeletedAt == null, ct);

            if (!branchBelongsToCompany)
                return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("La sucursal seleccionada no pertenece a la empresa actual.", "ERP:013");
        }

        var from = DateOnly.FromDateTime(request.From.Value);
        var to = DateOnly.FromDateTime(request.To.Value);
        var generatedBy = access.User?.Fullname ?? await GetGeneratedByAsync(request.UserId, ct);

        var movements = await LoadPeriodMovementsAsync(
            request,
            branchId,
            isAdministrator,
            isManager,
            access.Profile?.CostCenterId,
            from,
            to,
            ct);

        var costHistory = await LoadHistoricalCostsAsync(movements, to, ct);

        var entrySources = await BuildEntrySourcesAsync(movements, branchId, costHistory, ct);
        var exitSources = BuildExitSources(movements, costHistory);

        var entries = mapper.Map<List<InventoryMovementEntryDto>>(entrySources);
        var exits = mapper.Map<List<InventoryMovementExitDto>>(exitSources);

        var totalEntrada = new InventoryMovementTotalsDto
        {
            Count = entries.Count,
            Units = entries.Sum(i => i.Units),
            Total = entries.Sum(i => i.Total)
        };

        var totalSalida = new InventoryMovementTotalsDto
        {
            Count = exits.Count,
            Units = exits.Sum(i => i.Units),
            Total = exits.Sum(i => i.Total)
        };

        return new InventoryMovementReportDto
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From.Value,
            To = request.To.Value,
            TotalEntrada = totalEntrada,
            TotalSalida = totalSalida,
            MovimientoProducto = new InventoryMovementTotalsDto
            {
                Count = totalEntrada.Count + totalSalida.Count,
                Units = totalEntrada.Units + totalSalida.Units,
                Total = totalEntrada.Total + totalSalida.Total
            },
            Entradas = entries,
            Salidas = exits
        };
    }

    private async Task<List<Kardex>> LoadPeriodMovementsAsync(
        GetInventoryMovementReportQuery request,
        Guid branchId,
        bool isAdministrator,
        bool isManager,
        Guid? operatorCostCenterId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var query = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Include(k => k.Product)
            .Include(k => k.CostCenter)
            .Include(k => k.InventoryEntry)
                .ThenInclude(e => e!.PurchaseOrder)
                    .ThenInclude(po => po!.Supplier)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.SupplyRequest)
                    .ThenInclude(s => s!.OperationalOrder)
            .Include(k => k.InventoryOutput)
                .ThenInclude(o => o!.FuelRequest)
            .Where(k => k.DeletedAt == null)
            .Where(k => k.TransactionDate >= from && k.TransactionDate <= to)
            .Where(k => k.MovementType == KardexMovementType.Buys || k.MovementType == KardexMovementType.Exit)
            .Where(k =>
                (k.MovementType == KardexMovementType.Buys && k.CostCenter.BranchId == branchId) ||
                (k.MovementType == KardexMovementType.Exit && k.InventoryOutput != null && k.InventoryOutput.BranchId == branchId))
            .Where(k =>
                k.MovementType != KardexMovementType.Exit ||
                (k.InventoryOutput != null &&
                 k.InventoryOutput.Status == InventoryOutputsStatus.Dispatched &&
                 k.InventoryOutput.DeletedAt == null));

        if (isAdministrator)
        {
            if (request.CostCenterId.HasValue)
                query = query.Where(k => k.CostCenterId == request.CostCenterId.Value);
        }
        else if (isManager)
        {
            query = query.Where(k => k.CostCenter.BranchId == branchId && k.CostCenter.IsOperational);
            if (request.CostCenterId.HasValue)
                query = query.Where(k => k.CostCenterId == request.CostCenterId.Value);
        }
        else
        {
            query = query.Where(k => k.CostCenterId == operatorCostCenterId);
        }

        if (request.ProductId.HasValue)
            query = query.Where(k => k.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            var trimmedCode = request.ProductCode.Trim();
            query = query.Where(k => k.Product != null && k.Product.Code == trimmedCode);
        }

        return await query
            .OrderBy(k => k.TransactionDate)
            .ThenBy(k => k.TransactionTime)
            .ThenBy(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    private async Task<Dictionary<(Guid ProductId, Guid CostCenterId), (decimal MinCost, decimal MaxCost)>> LoadHistoricalCostsAsync(
        List<Kardex> movements,
        DateOnly to,
        CancellationToken ct)
    {
        if (movements.Count == 0)
            return [];

        var productIds = movements.Select(m => m.ProductId).Distinct().ToList();
        var costCenterIds = movements.Select(m => m.CostCenterId).Distinct().ToList();

        var historicalCosts = await _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Where(k => k.DeletedAt == null)
            .Where(k => k.TransactionDate <= to)
            .Where(k => productIds.Contains(k.ProductId) && costCenterIds.Contains(k.CostCenterId))
            .Where(k =>
                k.MovementType != KardexMovementType.Exit ||
                (k.InventoryOutput != null &&
                 k.InventoryOutput.Status == InventoryOutputsStatus.Dispatched &&
                 k.InventoryOutput.DeletedAt == null))
            .GroupBy(k => new { k.ProductId, k.CostCenterId })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.CostCenterId,
                MinCost = g.Min(k => k.UnitCost),
                MaxCost = g.Max(k => k.UnitCost)
            })
            .ToListAsync(ct);

        return historicalCosts.ToDictionary(
            x => (x.ProductId, x.CostCenterId),
            x => (x.MinCost, x.MaxCost));
    }

    private async Task<List<InventoryMovementEntrySource>> BuildEntrySourcesAsync(
        List<Kardex> movements,
        Guid branchId,
        Dictionary<(Guid ProductId, Guid CostCenterId), (decimal MinCost, decimal MaxCost)> costHistory,
        CancellationToken ct)
    {
        var entryMovements = movements
            .Where(k => k.MovementType == KardexMovementType.Buys)
            .ToList();

        if (entryMovements.Count == 0)
            return [];

        List<EntryCandidate>? fallbackCandidates = null;
        if (entryMovements.Any(m => m.InventoryEntry == null))
        {
            var fallbackEntries = await _unitOfWork.InventoryEntries.Entities
                .AsNoTracking()
                .Include(e => e.PurchaseOrder)
                    .ThenInclude(po => po!.Supplier)
                .Where(e => e.DeletedAt == null)
                .Where(e => e.BranchId == branchId)
                .Where(e => e.PurchaseOrder == null || e.PurchaseOrder.DeletedAt == null)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            fallbackCandidates = fallbackEntries
                .Select(entry =>
                {
                    var parsed = DeserializeAdditionalData(entry.AdditionalData);
                    return new EntryCandidate
                    {
                        Entry = entry,
                        RootSupplierName = parsed.SupplierName,
                        Items = parsed.Items,
                        Used = new bool[parsed.Items.Count]
                    };
                })
                .ToList();
        }

        var additionalDataCache = new Dictionary<Guid, PurchaseOrderEntryAdditionalData>();
        var sources = new List<InventoryMovementEntrySource>(entryMovements.Count);

        foreach (var movement in entryMovements)
        {
            costHistory.TryGetValue((movement.ProductId, movement.CostCenterId), out var costs);

            InventoryEntry? entry = movement.InventoryEntry;
            PurchaseOrderEntryAdditionalItem? additionalItem = null;
            string? rootSupplierName = null;

            if (entry != null)
            {
                if (!additionalDataCache.TryGetValue(entry.Id, out var parsed))
                {
                    parsed = DeserializeAdditionalData(entry.AdditionalData);
                    additionalDataCache[entry.Id] = parsed;
                }

                rootSupplierName = parsed.SupplierName;
                additionalItem = parsed.Items.FirstOrDefault(i =>
                    i.ProductId == movement.ProductId &&
                    (i.CostCenterId == null || i.CostCenterId == movement.CostCenterId));
            }
            else if (fallbackCandidates != null)
            {
                (entry, additionalItem, rootSupplierName) = MatchEntry(movement, fallbackCandidates);
            }

            sources.Add(new InventoryMovementEntrySource
            {
                Kardex = movement,
                Entry = entry,
                AdditionalItem = additionalItem,
                RootSupplierName = rootSupplierName,
                MinimumCost = costs.MinCost,
                MaximumCost = costs.MaxCost
            });
        }

        return sources;
    }

    private static List<InventoryMovementExitSource> BuildExitSources(
        List<Kardex> movements,
        Dictionary<(Guid ProductId, Guid CostCenterId), (decimal MinCost, decimal MaxCost)> costHistory)
    {
        var exitMovements = movements
            .Where(k => k.MovementType == KardexMovementType.Exit)
            .ToList();

        var sources = new List<InventoryMovementExitSource>(exitMovements.Count);

        foreach (var movement in exitMovements)
        {
            costHistory.TryGetValue((movement.ProductId, movement.CostCenterId), out var costs);
            var output = movement.InventoryOutput!;

            sources.Add(new InventoryMovementExitSource
            {
                Kardex = movement,
                OutputSource = new OutputReportSource
                {
                    Output = output,
                    Kardex = movement,
                    IsFuel = output.FuelRequest != null,
                    Item = null
                },
                MaximumCost = costs.MaxCost
            });
        }

        return sources;
    }

    private static (InventoryEntry? Entry, PurchaseOrderEntryAdditionalItem? Item, string? RootSupplierName) MatchEntry(
        Kardex movement,
        List<EntryCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            for (var i = 0; i < candidate.Items.Count; i++)
            {
                if (candidate.Used[i])
                    continue;

                var item = candidate.Items[i];
                if (item.ProductId == movement.ProductId &&
                    (item.CostCenterId == null || item.CostCenterId == movement.CostCenterId) &&
                    Math.Abs(item.UnitPrice - movement.UnitCost) <= 0.01m)
                {
                    candidate.Used[i] = true;
                    return (candidate.Entry, item, candidate.RootSupplierName);
                }
            }
        }

        return (null, null, null);
    }

    private static PurchaseOrderEntryAdditionalData DeserializeAdditionalData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new PurchaseOrderEntryAdditionalData();

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
}
