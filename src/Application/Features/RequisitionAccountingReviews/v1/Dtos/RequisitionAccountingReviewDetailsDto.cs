using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class RequisitionAccountingReviewDetailsDto
    {
        public Guid RequisitionAccountingReviewId { get; set; }
        public string? Comments { get; set; }
        public AccountingReviewStatus Status { get; set; }
        public Guid? ReviewedByUserId { get; set; }

        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new();

        public PurchaseRequestRawInformationDto PurchaseRequest { get; set; } = new();
    }

    public class PurchaseRequestRawInformationDto
    {
        public bool IsActive { get; set; }
        public Guid PurchaseRequestId { get; set; }
        public string? Code { get; set; }
        public string? Observations { get; set; }

        public DateOnly RequestDate { get; set; }
        public DateOnly? RevisionDate { get; set; }
        public PurchaseRequestType RequestType { get; set; }
        public PurchaseRequestStatus RequestStatus { get; set; }

        public string? ReasonRejection { get; set; }

        public Guid? UserRevisionId { get; set; }
        public Guid RegisteredByUserId { get; set; }
        public Guid BranchId { get; set; }
        public Guid AreaId { get; set; }

        public BranchInformation BranchInformation { get; set; } = new();
        public WorkAreaInformation WorkAreaInformation { get; set; } = new();
        public CreatorUserInformation CreatorUserInformation { get; set; } = new();
        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new();

        public List<PurchaseRequestItemRawInformationDto> RequestedProducts { get; set; } = [];
    }

    public class PurchaseRequestItemRawInformationDto
    {
        public Guid PurchaseRequestItemId { get; set; }
        public bool HasQuotation { get; set; }

        public int Quantity { get; set; }
        public int? QuantityUnit { get; set; }

        public string? Description { get; set; }
        public string? Justification { get; set; }

        public Guid PurchaseRequestId { get; set; }

        public ProductDetails ProductDetails { get; set; } = new();
        public UnitMeasureInformation UnitMeasureInformation { get; set; } = new();

        public List<QuotationInformationDto> Quotations { get; set; } = [];
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

    public class WorkAreaInformation
    {
        public Guid WorkAreaId { get; set; }
        public int WorkAreaCode { get; set; }
        public string? Description { get; set; }
        public string? WorkAreaName { get; set; }
    }
}
