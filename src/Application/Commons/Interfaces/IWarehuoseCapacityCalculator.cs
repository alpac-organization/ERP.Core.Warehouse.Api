namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

public sealed record WarehouseAreaCapacity(
    decimal TotalAreaM2,
    decimal UsableAreaM2,
    decimal UnusableAreaM2,
    decimal OccupiedAreaM2,
    decimal FreeAreaM2,
    decimal OccupancyPercentage,
    int TotalPositions,
    int UsedPositions,
    int FreePositions);

public interface IWarehouseCapacityCalculator
{
    WarehouseAreaCapacity Calculate(WarehouseEntity warehouse);
    Task PersistCalculatedCapacityAsync(WarehouseEntity warehouse, CancellationToken cancellationToken);
}