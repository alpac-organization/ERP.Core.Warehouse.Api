using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services;

public class WarehouseCapacityCalculator : IWarehouseCapacityCalculator
{
    public WarehouseAreaCapacity Calculate(WarehouseEntity warehouse)
    {
        var rackMetrics = PositionMetrics.Summarize<Racks, RackPositions>(
            warehouse.Sections?.SelectMany((Sections section) => section.Racks ?? []),
            (Racks rack) => rack.Positions,
            (RackPositions position) => position.IsOccupied || position.IsBlocked,
            (Racks rack) => rack.WidthMetres,
            (Racks rack) => rack.LengthMetres);
        var lotMetrics = PositionMetrics.Summarize<Lots, LotsPositions>(
            warehouse.Sections?.SelectMany((Sections section) => section.Lots ?? []),
            (Lots lot) => lot.Positions,
            (LotsPositions position) => position.IsOccupied || position.IsBlocked,
            (Lots lot) => lot.WidthMetres,
            (Lots lot) => lot.LengthMetres);

        var usableAreaM2 = rackMetrics.TotalAreaM2 + lotMetrics.TotalAreaM2;
        var totalAreaM2 = warehouse.Details is null
            ? warehouse.Capacity?.TotalAreaM2 ?? 0
            : PositionMetrics.Area(
                warehouse.Details.WitdhMetres,
                warehouse.Details.LengthMetres);
        var unusableAreaM2 = Math.Max(0, totalAreaM2 - usableAreaM2);
        var occupiedAreaM2 = rackMetrics.UsedAreaM2 + lotMetrics.UsedAreaM2;
        var freeAreaM2 = Math.Max(0, usableAreaM2 - occupiedAreaM2);
        var occupancyPercentage = usableAreaM2 > 0
            ? Math.Round(occupiedAreaM2 / usableAreaM2 * 100, 2)
            : 0;
        var totalPositions = rackMetrics.TotalPositions + lotMetrics.TotalPositions;
        var usedPositions = (warehouse.Sections ?? []).Sum(GetSectionOccupiedPositions);

        return new WarehouseAreaCapacity(
            totalAreaM2,
            usableAreaM2,
            unusableAreaM2,
            occupiedAreaM2,
            freeAreaM2,
            occupancyPercentage,
            totalPositions,
            usedPositions,
            Math.Max(0, totalPositions - usedPositions));
    }

    private static int GetSectionOccupiedPositions(Sections section)
    {
        var rackOccupied = GetRacksOccupiedPositions(section.Racks);
        var lotOccupied = GetLotsOccupiedPositions(section.Lots);
        return rackOccupied + lotOccupied;
    }

    private static int GetRacksOccupiedPositions(IEnumerable<Racks>? racks) =>
        (racks ?? []).Sum(GetRackOccupiedPositions);

    private static int GetRackOccupiedPositions(Racks rack) =>
        PositionMetrics.Occupied(rack.Positions,
            (RackPositions position) => position.IsOccupied || position.IsBlocked);

    private static int GetLotsOccupiedPositions(IEnumerable<Lots>? lots) =>
        (lots ?? []).Sum(GetLotOccupiedPositions);

    private static int GetLotOccupiedPositions(Lots lot) =>
        PositionMetrics.Occupied(lot.Positions,
            (LotsPositions position) => position.IsOccupied || position.IsBlocked);
}