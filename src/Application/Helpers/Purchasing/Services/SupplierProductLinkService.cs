using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public class SupplierProductLinkService : ISupplierProductLinkService
    {
        public SupplierProduct CreateLink(Guid productId, Guid supplierId, Guid unitMeasureId, decimal unitPrice, Currency currency)
        {
            return new SupplierProduct
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                ProductId = productId,
                SupplierId = supplierId,
                UnitMeasureId = unitMeasureId,
                UnitPrice = unitPrice,
                Currency = currency,
                LastPriceUpdate = DateTime.UtcNow
            };
        }

        public SupplierProduct? FindActiveLink(Supplier supplier, Guid productId)
        {
            return supplier.SupplierProducts.FirstOrDefault(link =>
                link.ProductId == productId &&
                link.IsActive &&
                link.DeletedAt == null);
        }

        public Task<bool> InactiveLinkExistsAsync(IQueryable<SupplierProduct> supplierProducts, Guid productId, Guid supplierId, CancellationToken cancellationToken)
        {
            return supplierProducts
                .IgnoreQueryFilters()
                .AnyAsync(
                    link => link.ProductId == productId
                        && link.SupplierId == supplierId
                        && (!link.IsActive || link.DeletedAt != null),
                    cancellationToken);
        }

        public decimal? ResolveQuotationUnitPrice(
            SupplierProduct supplierProduct,
            int quantity,
            DateOnly quoteDate,
            IPurchaseTaxService purchaseTaxService)
        {
            var catalogPrice = purchaseTaxService.ResolveUnitPrice(supplierProduct, quantity, quoteDate);
            if (catalogPrice > 0)
            {
                return catalogPrice;
            }

            return null;
        }
    }
}
