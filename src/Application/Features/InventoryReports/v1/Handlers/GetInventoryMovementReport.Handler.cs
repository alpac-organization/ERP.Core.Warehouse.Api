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

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (!isAdministrator && access.Profile == null)
            return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("Perfil no encontrado.", "ERP:011");

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<InventoryMovementReportDto>("Debe especificar una sucursal para consultar los movimientos.", "ERP:007");

        var branchId = isAdministrator ? request.BranchId!.Value : access.Profile.BranchId;
        var from = DateOnly.FromDateTime(request.From.Value);
        var to = DateOnly.FromDateTime(request.To.Value);
        var generatedBy = await GetGeneratedByAsync(request.UserId, ct);

        var movements = await LoadPeriodMovementsAsync(request, branchId, from, to, ct);
        var costHistory = await LoadHistoricalCostsAsync(request, branchId, to, ct);

        var entrySources = await BuildEntrySourcesAsync(movements, request, branchId, costHistory, ct);
        var exitSources = BuildExitSources(movements, costHistory);

        var entries = mapper.Map<List<InventoryMovementEntryDto>>(entrySources);
        var exits = mapper.Map<List<InventoryMovementExitDto>>(exitSources);

        return new InventoryMovementReportDto
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From.Value,
            To = request.To.Value,
            TotalEntrada = new InventoryMovementTotalsDto
            {
                Units = entries.Sum(i => i.Units),
                Total = entries.Sum(i => i.Total)
            },
            MovimientoProducto = new InventoryMovementTotalsDto
            {
                Units = exits.Sum(i => i.Units),
                Total = exits.Sum(i => i.Total)
            },
            Entradas = entries,
            Salidas = exits
        };
    }

    private async Task<List<Kardex>> LoadPeriodMovementsAsync(
        GetInventoryMovementReportQuery request,
        Guid branchId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var query = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Include(k => k.Product)
            .Include(k => k.CostCenter)
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

        if (request.CostCenterId.HasValue)
            query = query.Where(k => k.CostCenterId == request.CostCenterId.Value);

        if (request.ProductId.HasValue)
            query = query.Where(k => k.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.ProductCode))
            query = query.Where(k => k.Product != null && k.Product.Code == request.ProductCode);

        return await query
            .OrderBy(k => k.TransactionDate)
            .ThenBy(k => k.TransactionTime)
            .ThenBy(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    private async Task<Dictionary<(Guid ProductId, Guid CostCenterId), (decimal MinCost, decimal MaxCost)>> LoadHistoricalCostsAsync(
        GetInventoryMovementReportQuery request,
        Guid branchId,
        DateOnly to,
        CancellationToken ct)
    {
        var query = _unitOfWork.Kardex.Entities
            .AsNoTracking()
            .Where(k => k.DeletedAt == null)
            .Where(k => k.TransactionDate <= to)
            .Where(k => k.CostCenter.BranchId == branchId);

        if (request.CostCenterId.HasValue)
            query = query.Where(k => k.CostCenterId == request.CostCenterId.Value);

        if (request.ProductId.HasValue)
            query = query.Where(k => k.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.ProductCode))
            query = query.Where(k => k.Product != null && k.Product.Code == request.ProductCode);

        var historicalCosts = await query
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
        GetInventoryMovementReportQuery request,
        Guid branchId,
        Dictionary<(Guid ProductId, Guid CostCenterId), (decimal MinCost, decimal MaxCost)> costHistory,
        CancellationToken ct)
    {
        var entryMovements = movements
            .Where(k => k.MovementType == KardexMovementType.Buys)
            .ToList();

        if (entryMovements.Count == 0)
            return [];

        // Kardex usa fecha/hora local de Nicaragua (UTC-6); CreatedAt se persiste en UTC.
        var fromUtc = request.From!.Value.Date.AddHours(6);
        var toUtc = request.To!.Value.Date.AddDays(1).AddHours(6);

        var entries = await _unitOfWork.InventoryEntries.Entities
            .AsNoTracking()
            .Include(e => e.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Where(e => e.DeletedAt == null)
            .Where(e => e.BranchId == branchId)
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt <= toUtc)
            .Where(e => e.PurchaseOrder == null || e.PurchaseOrder.DeletedAt == null)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        var candidates = entries
            .Select(entry =>
            {
                var items = DeserializeAdditionalData(entry.AdditionalData).Items.ToList();
                return new EntryCandidate
                {
                    Entry = entry,
                    Items = items,
                    Used = new bool[items.Count]
                };
            })
            .ToList();

        var sources = new List<InventoryMovementEntrySource>(entryMovements.Count);

        foreach (var movement in entryMovements)
        {
            costHistory.TryGetValue((movement.ProductId, movement.CostCenterId), out var costs);

            var (entry, additionalItem) = MatchEntry(movement, candidates);

            sources.Add(new InventoryMovementEntrySource
            {
                Kardex = movement,
                Entry = entry,
                AdditionalItem = additionalItem,
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

    private static (InventoryEntry? Entry, PurchaseOrderEntryAdditionalItem? Item) MatchEntry(
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
                    item.CostCenterId == movement.CostCenterId &&
                    item.Quantity == movement.Units &&
                    item.UnitPrice == movement.UnitCost)
                {
                    candidate.Used[i] = true;
                    return (candidate.Entry, item);
                }
            }
        }

        return (null, null);
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

    // TODO: Retomar corrección del proveedor en entradas.
    //
    // Situación actual:
    // - Kardex no tiene navegación a InventoryEntry ni a PurchaseOrderItem.
    // - Solo tiene navegación directa a InventoryOutput, usada por las salidas.
    // - El proveedor de una entrada se obtiene hoy de:
    //     InventoryEntry.PurchaseOrder.Supplier
    //     InventoryEntry.AdditionalData.SupplierName
    //     InventoryEntry.AdditionalData.Items[].SupplierName
    // - MatchEntry puede devolver (null, null), por lo que document_code,
    //   reference y supplier quedan null aunque los datos existan en BD.
    //
    // Corrección pendiente (mínima):
    // 1. Quitar en BuildEntrySourcesAsync:
    //      .Where(e => e.BranchId == branchId)
    //      .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt <= toUtc)
    //    CreatedAt es la creación de la entrada pendiente, no el registro del Kardex.
    //    BranchId se asigna desde el perfil, no desde el centro de costo del Kardex.
    // 2. Cargar entradas aprobadas, sin borrar, con PurchaseOrder -> Supplier.
    // 3. Hacer MatchEntry tolerante con decimales:
    //      ProductId + CostCenterId + Units + UnitCost
    //    y marcar cada ítem como usado para no repetirlo.
    // 4. Pasar al source el AdditionalData.SupplierName del nivel raíz y usarlo
    //    como fallback antes de AdditionalItem.SupplierName.
    //
    // Fallbacks adicionales desde Product:
    // - Kardex -> Product -> PurchaseOrderItems -> PurchaseOrder -> Supplier,
    //   intentando emparejar por ProductId + Quantity + UnitCost.
    // - Kardex -> Product -> SupplierProducts -> Supplier solo si hay
    //   exactamente un proveedor activo para el producto.
    //
    // Corrección definitiva:
    // - Agregar Kardex.InventoryEntryId y Kardex.PurchaseOrderItemId (nullable).
    // - Configurar las navegaciones InventoryEntry y PurchaseOrderItem.
    // - Setear ambos en RegisterInventoryEntry.Handler al crear el Kardex.
    // - Crear migración con backfill para los Kardex históricos.
    // - Cambiar el handler a Include(k => k.InventoryEntry)... y eliminar MatchEntry.
}
