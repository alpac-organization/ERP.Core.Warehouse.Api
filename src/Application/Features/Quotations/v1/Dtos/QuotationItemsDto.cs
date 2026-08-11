using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Dtos
{
    public class QuotationItems
    {
        public Guid QuotationId { get; set; }

        public bool IsActive { get; set; }
        public bool HasDelivery { get; set; }
        public bool HasGuarantee { get; set; }

        public decimal Iva { get; set; }
        public decimal Price { get; set; }
        public decimal PriceUnit { get; set; }
        public decimal PriceTotal { get; set; }

        public DateOnly QuoteDate { get; set; }
        public string? BrandProduct { get; set; }

        public decimal? DeliveryTime { get; set; }
        public TimeType? DeliveryTimeType { get; set; }

        public decimal? WarrantyPeriod { get; set; }
        public TimeType? WarrantyPeriodTimeType { get; set; }

        public Guid PurchaseRequestItemId { get; set; }
        public Guid SupplierId { get; set; }
        public SupplierInformationDto SupplierInformation { get; set; } = new();
    }
}
