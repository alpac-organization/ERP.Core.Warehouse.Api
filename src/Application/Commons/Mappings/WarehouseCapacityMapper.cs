using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

internal static class WarehouseCapacityMapper
{
    public static void Apply(WarehouseDto warehouse, WarehouseAreaCapacity capacity)
    {
        if (warehouse.Capacity is null)
            return;

        warehouse.Capacity.TotalAreaM2 = capacity.TotalAreaM2;
        warehouse.Capacity.UsableAreaM2 = capacity.UsableAreaM2;
        warehouse.Capacity.UnusableAreaM2 = capacity.UnusableAreaM2;
        warehouse.Capacity.OccupiedAreaM2 = capacity.OccupiedAreaM2;
        warehouse.Capacity.FreeAreaM2 = capacity.FreeAreaM2;
        warehouse.Capacity.OccupancyPercentage = capacity.OccupancyPercentage;
        warehouse.Capacity.TotalPositions = capacity.TotalPositions;
        warehouse.Capacity.UsedPositions = capacity.UsedPositions;
        warehouse.Capacity.FreePositions = capacity.FreePositions;
    }
}