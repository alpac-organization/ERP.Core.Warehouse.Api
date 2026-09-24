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

        public string? AdditionalData {get; set;}

        public ProductDetails ProductDetails { get; set; } = new();
        public UnitMeasureInformation UnitMeasureInformation { get; set; } = new();

        public List<QuotationInformationDto> Quotations { get; set; } = [];
    }

    public class ProductDetails
    {
        // public string? ProductCode { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public CategoryInformation CategoryInformation { get; set; } = new();
    }

    public class CategoryInformation
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public Guid CatagoryId { get; set; }
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
        public bool InventoryAvailable { get; set; }
        public bool IsAcceptedForPurchase { get; set; }
        
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

        public string? SupplierSelectionJustification { get; set; }
        public string? SupplierRejectionJustification { get; set; }

        public ProductQuality ProductQuality { get; set; }
        public PaymentCondition PaymentCondition { get; set; }

        public decimal? AvailabilityTime { get; set; }
        public TimeType? AvailabilityTimeType { get; set; }

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
        public SupplierDetailsInformation SupplierDetailsInformation { get; set; } = new();
        public List<SupplierBankAccounts> SupplierBankAccounts { get; set; } = [];
    }

    public class SupplierBankAccounts
    {
        public Currency Currency { get; set; }
        public BankAccountType AccountType { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string? AccountHolderIdentification { get; set; }
        public bool IsPrimary { get; set; } = false;
    }

    public class SupplierDetailsInformation
    {
        public bool IsExclusive { get; set; }
        public string? ExclusiveBrandsOrParts { get; set; }

        public int CreditDays { get; set; }
        public bool HasCredit { get; set; }

        public decimal? CreditLimit { get; set; }
        public int AlertDaysBeforeDue { get; set; }
        public Currency? CreditCurrency { get; set; }
        public PaymentMethodType PreferredPaymentMethod { get; set; }

        public bool ApplyIrRetention { get; set; }
        public bool ApplyMunicipalRetention { get; set; }
        public bool IsTaxExempt { get; set; }
    }
}
