using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Inventory;

public static class FuelRequestSharedHelper
{
    public static async Task<(decimal? PreviousMileage, decimal? PreviousEngineHours)> GetPreviousFuelRequestReadingsAsync(
        IUnitOfWork unitOfWork,
        Guid equipmentId,
        Guid currentFuelRequestId,
        DateTime currentCreatedAt,
        CancellationToken ct)
    {
        var previous = await unitOfWork.FuelRequests.Entities
            .AsNoTracking()
            .Where(fr => fr.EquipmentId == equipmentId
                      && fr.Id != currentFuelRequestId
                      && fr.DeletedAt == null
                      && (fr.CreatedAt < currentCreatedAt
                          || (fr.CreatedAt == currentCreatedAt && fr.Id < currentFuelRequestId)))
            .OrderByDescending(fr => fr.CreatedAt)
            .ThenByDescending(fr => fr.Id)
            .Select(fr => new { fr.Mileage, fr.EngineHours })
            .FirstOrDefaultAsync(ct);

        return (previous?.Mileage, previous?.EngineHours);
    }
}