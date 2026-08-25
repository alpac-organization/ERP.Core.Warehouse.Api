using Microsoft.EntityFrameworkCore;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

namespace ERP.Core.Warehouse.Api.Application.Commons.Extensions;

internal static class WarehouseQueryExtensions
{
    public static IQueryable<WarehouseEntity> IncludeWarehouseDetails(
        this IQueryable<WarehouseEntity> query) =>
        query
            .AsSplitQuery()
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .Include(w => w.Details)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Racks)
                    .ThenInclude(r => r.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Lots)
                    .ThenInclude(l => l.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Capacity);
}