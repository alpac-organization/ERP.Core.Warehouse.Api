namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

public sealed record WarehouseAreaCapacity(
    decimal OccupiedAreaM2,
    decimal FreeAreaM2,
    decimal OccupancyPercentage);

public interface IWarehouseCapacityCalculator
{
    Task<WarehouseAreaCapacity> CalculateAsync(
        Guid warehouseId,
        decimal totalAreaM2,
        decimal? usableAreaM2,
        CancellationToken cancellationToken);
}