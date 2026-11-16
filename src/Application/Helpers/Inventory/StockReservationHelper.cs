using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Inventory;

public static class StockReservationHelper
{
    public static async Task<List<(string Name, string Code, decimal Available, decimal Minimum)>> ReserveStockAsync(
        IUnitOfWork unitOfWork,
        IReadOnlyDictionary<Guid, decimal> reservedByStock,
        CancellationToken ct)
    {
        var lowStockItems = new List<(string Name, string Code, decimal Available, decimal Minimum)>();

        foreach (var (stockId, qty) in reservedByStock)
        {
            var stock = await unitOfWork.ProductStocks.Entities
                .Include(ps => ps.Product)
                .FirstOrDefaultAsync(ps => ps.Id == stockId && ps.DeletedAt == null, ct);

            if (stock is null) continue;

            stock.AvailableQuantity -= qty;

            if (stock.AvailableQuantity <= 0)
                stock.Status = ProductStockStatus.Empty;
            else if (stock.AvailableQuantity <= stock.MinimumStock)
                stock.Status = ProductStockStatus.LowBalance;

            await unitOfWork.ProductStocks.UpdateAsync(stock);

            if (stock.Status is ProductStockStatus.LowBalance or ProductStockStatus.Empty)
            {
                lowStockItems.Add((
                    stock.Product?.ProductName ?? stock.Product?.Code ?? "Producto",
                    stock.Product?.Code ?? "-",
                    stock.AvailableQuantity,
                    stock.MinimumStock));
            }
        }

        return lowStockItems;
    }

    public static async Task RevertStockAsync(
        IUnitOfWork unitOfWork,
        IReadOnlyDictionary<Guid, decimal> revertedByStock,
        CancellationToken ct)
    {
        foreach (var (stockId, qty) in revertedByStock)
        {
            var stock = await unitOfWork.ProductStocks.Entities
                .FirstOrDefaultAsync(ps => ps.Id == stockId && ps.DeletedAt == null, ct);

            if (stock is null) continue;

            stock.AvailableQuantity += qty;

            if (stock.AvailableQuantity <= 0)
                stock.Status = ProductStockStatus.Empty;
            else if (stock.AvailableQuantity <= stock.MinimumStock)
                stock.Status = ProductStockStatus.LowBalance;
            else
                stock.Status = ProductStockStatus.Stable;

            await unitOfWork.ProductStocks.UpdateAsync(stock);
        }
    }
}
