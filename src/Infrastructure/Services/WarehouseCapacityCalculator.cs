using Microsoft.EntityFrameworkCore;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services;

public class WarehouseCapacityCalculator(IUnitOfWork unitOfWork) : IWarehouseCapacityCalculator
{
    public async Task<WarehouseAreaCapacity> CalculateAsync(
        Guid warehouseId,
        decimal totalAreaM2,
        decimal? usableAreaM2,
        CancellationToken cancellationToken)
    {
        var occupiedAreaM2 = await CalculateOccupiedAreaAsync(warehouseId, cancellationToken);
        var usableArea = usableAreaM2 ?? totalAreaM2;
        var freeAreaM2 = usableArea - occupiedAreaM2;
        var occupancyPercentage = usableArea > 0
            ? Math.Round(occupiedAreaM2 / usableArea * 100, 2)
            : 0;

        return new WarehouseAreaCapacity(occupiedAreaM2, freeAreaM2, occupancyPercentage);
    }

    private async Task<decimal> CalculateOccupiedAreaAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var rackAreas = await unitOfWork.Racks.Entities
            .Where(r => r.Section.WarehouseId == warehouseId)
            .Select(r => new
            {
                AreaTotal = r.WidthMetres * r.LengthMetres,
                PositionsCount = r.Positions.Count,
                OccupiedCount = r.Positions.Count(p => p.CurrentStock.Count > 0)
            })
            .ToListAsync(cancellationToken);

        var lotAreas = await unitOfWork.Lots.Entities
            .Where(l => l.Section.WarehouseId == warehouseId)
            .Select(l => new
            {
                AreaTotal = l.WidthMetres * l.LengthMetres,
                PositionsCount = l.Positions.Count,
                OccupiedCount = l.Positions.Count(p => p.CurrentStock != null)
            })
            .ToListAsync(cancellationToken);

        var occupied = rackAreas
            .Where(r => r.PositionsCount > 0)
            .Sum(r => (r.AreaTotal / r.PositionsCount) * r.OccupiedCount);

        occupied += lotAreas
            .Where(l => l.PositionsCount > 0)
            .Sum(l => (l.AreaTotal / l.PositionsCount) * l.OccupiedCount);

        return occupied;
    }
}