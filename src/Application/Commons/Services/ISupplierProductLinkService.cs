using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    /// Crea y resuelve vínculos proveedor–producto con precios exclusivos del vínculo.
    public interface ISupplierProductLinkService
    {
        SupplierProduct CreateLink(
            Guid productId,
            Guid supplierId,
            Guid unitMeasureId,
            decimal unitPrice,
            Currency currency,
            decimal? preferentialPrice,
            DateOnly validFrom);

        // Resuelve el precio unitario de cotización
        decimal? ResolveQuotationUnitPrice(
            SupplierProduct supplierProduct,
            int quantity,
            DateOnly quoteDate,
            decimal? requestPriceUnit,
            IPurchaseTaxService purchaseTaxService);
    }
}
