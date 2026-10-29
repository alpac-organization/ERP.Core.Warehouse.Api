using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces
{
    public interface ISupplierProductLinkService
    {
        // arma el vínculo nuevo entre supplier-product cuando la cotización no encuentra un vinculo.
        SupplierProduct CreateLink(Guid productId, Guid supplierId, Guid unitMeasureId, decimal unitPrice, Currency currency);

        // Devuelve el vínculo activo de ese producto dentro de los vínculos ya cargados del supplier.
        SupplierProduct? FindActiveLink(Supplier supplier, Guid productId);

        // Indica si existe un vínculo inactivo.
        Task<bool> InactiveLinkExistsAsync(IQueryable<SupplierProduct> supplierProducts, Guid productId, Guid supplierId, CancellationToken cancellationToken);

        decimal? ResolveQuotationUnitPrice(
            SupplierProduct supplierProduct,
            int quantity,
            DateOnly quoteDate,
            IPurchaseTaxService purchaseTaxService,
            decimal? requestPriceUnit = null,
            Currency? requestCurrency = null);
    }
}
