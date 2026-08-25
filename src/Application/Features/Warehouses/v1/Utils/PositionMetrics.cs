namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Utils;

public static class PositionMetrics
{
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
}