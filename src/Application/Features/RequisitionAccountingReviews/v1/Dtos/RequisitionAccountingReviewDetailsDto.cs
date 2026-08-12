using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class RequisitionAccountingReviewDetailsDto : RequisitionAccountingReviewDto
    {
        public Guid? ReviewedByUserId { get; set; }
        public PurchaseRequestDetailsDto PurchaseRequest { get; set; } = new();
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
        
        public List<CostCenterInformation>? CostCenters { get; set; } = [];
    }

    public class CostCenterInformation
    {
        public Guid CostCenterId { get; set; }
        public string? Description { get; set; }
        public string? CostCenterName { get; set; }
        public int CoilCode { get; set; }
        public int CostCenterCode { get; set; }
    }
}
