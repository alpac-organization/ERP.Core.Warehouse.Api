namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Utils;

public static class PositionMetrics
{
    public sealed record Summary(
        int TotalPositions,
        int UsedPositions,
        decimal TotalAreaM2,
        decimal UsedAreaM2);

    public static int Total<T>(IEnumerable<T>? positions) => positions?.Count() ?? 0;

    public static int Occupied<T>(IEnumerable<T>? positions, Func<T, bool> selector) =>
        positions?.Count(selector) ?? 0;

    public static int Blocked<T>(IEnumerable<T>? positions, Func<T, bool> selector) =>
        positions?.Count(selector) ?? 0;

    public static int Free(int total, int occupied, int blocked) =>
        Math.Max(0, total - occupied - blocked);

    public static decimal OccupancyPercentage(int total, int used) =>
        total == 0 ? 0 : Math.Round(used * 100m / total, 2);

    public static decimal Area(decimal widthMetres, decimal lengthMetres) =>
        Math.Round(widthMetres * lengthMetres, 2);

    public static decimal UsedArea(decimal totalArea, int total, int used) =>
        total == 0 ? 0 : Math.Round(used * 1.0m / total * totalArea, 2);

    public static Summary Summarize<TContainer, TPosition>(
        IEnumerable<TContainer>? containers,
        Func<TContainer, IEnumerable<TPosition>?> positionsSelector,
        Func<TPosition, bool> usedSelector,
        Func<TContainer, decimal> widthSelector,
        Func<TContainer, decimal> lengthSelector)
    {
        var totalPositions = 0;
        var usedPositions = 0;
        var totalAreaM2 = 0m;

        foreach (var container in containers ?? [])
        {
            var positions = positionsSelector(container)?.ToList() ?? [];
            totalPositions += positions.Count;
            usedPositions += positions.Count(usedSelector);
            totalAreaM2 += Area(widthSelector(container), lengthSelector(container));
        }

        return new Summary(
            totalPositions,
            usedPositions,
            totalAreaM2,
            UsedArea(totalAreaM2, totalPositions, usedPositions));
    }
}