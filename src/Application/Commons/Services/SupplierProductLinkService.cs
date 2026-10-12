using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    /// <inheritdoc />
    public class SupplierProductLinkService : ISupplierProductLinkService
    {
        public SupplierProduct CreateLink(
            Guid productId,
            Guid supplierId,
            Guid unitMeasureId,
            decimal unitPrice,
            Currency currency,
            decimal? preferentialPrice,
            DateOnly validFrom)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
            ArgumentOutOfRangeException.ThrowIfEqual(supplierId, Guid.Empty);
            ArgumentOutOfRangeException.ThrowIfEqual(unitMeasureId, Guid.Empty);
            ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

            var now = DateTime.UtcNow;
            var supplierProductId = Guid.NewGuid();

            var supplierProduct = new SupplierProduct
            {
                Id = supplierProductId,
                IsActive = true,
                ProductId = productId,
                SupplierId = supplierId,
                UnitMeasureId = unitMeasureId,
                UnitPrice = unitPrice,
                Currency = currency,
                LastPriceUpdate = now
            };

            if (preferentialPrice is > 0)
            {
                supplierProduct.TierPrices.Add(new SupplierProductTierPrice
                {
                    Id = Guid.NewGuid(),
                    SupplierProductId = supplierProductId,
                    MinQuantity = 1,
                    PreferentialPrice = preferentialPrice.Value,
                    ValidFrom = validFrom,
                    ValidTo = null,
                    UnitMeasureId = unitMeasureId
                });
            }

            return supplierProduct;
        }

        public decimal? ResolveQuotationUnitPrice(
            SupplierProduct supplierProduct,
            int quantity,
            DateOnly quoteDate,
            decimal? requestPriceUnit,
            IPurchaseTaxService purchaseTaxService)
        {
            ArgumentNullException.ThrowIfNull(supplierProduct);
            ArgumentNullException.ThrowIfNull(purchaseTaxService);

            var catalogPrice = purchaseTaxService.ResolveUnitPrice(supplierProduct, quantity, quoteDate);
            if (catalogPrice > 0)
            {
                return catalogPrice;
            }

            if (requestPriceUnit is > 0)
            {
                return requestPriceUnit.Value;
            }

            return null;
        }
    }
}
