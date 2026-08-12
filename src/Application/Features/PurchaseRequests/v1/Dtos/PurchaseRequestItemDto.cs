using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestItemDto
    {
        public bool HasQuotation { get; set; }
        public Guid PurchaseRequestItemId { get; set; }

        public int Quantity { get; set; }
        public int? QuantityUnit { get; set; }

        public string? Description { get; set; }
        public string? Justification { get; set; }

        public ProductDetails ProductDetails { get; set; } = new ();
        public List<QuotationInformationDto> Quotations { get; set; } = [];
        public UnitMeasureInformation UnitMeasureInformation { get; set; } = new();
    }

    public class ProductDetails
    {
        // public string? ProductCode { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public CategoryInformation CategoryInformation { get; set; }= new ();
    } 

    public class CategoryInformation
    {
        public string? Name {get; set;}
        public string? Code {get; set;}
        public Guid CatagoryId { get; set;}
    }

    public class UnitMeasureInformation
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
    }

    public class QuotationInformationDto
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

        public Guid SupplierId { get; set; }
        public SupplierInformation SupplierInformation { get; set; } = new();
    }

    public class SupplierInformation
    {
        public Guid SupplierId { get; set; }
        public string? ImageUrl { get; set; }
        public string? SuppliersLegalName { get; set; }
        public string? IdentificationNumber { get; set; }
        public IdentificationType? IdentificationType { get; set; }
    }
}
