using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestDetailsDto : PurchaseRequestDto
    {
        public string? Observations { get; set; }
        public string? ReasonRejection { get; set; }

        public BranchInformation BranchInformation { get; set; } = new ();
        public WorkAreaInformation  InformationFromRequestingArea { get; set; } = new ();
        public CreatorUserInformation  CreatorUserInformation { get; set; } = new();
        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new ();
        public List<ProductInformation> RequestedProducts { get; set; }  =  [];

    }

    public class BranchInformation
    {
        public Guid BranchId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? CompanyAlias { get; set; }
    }

    public class ReviewerUserInformation : SentByUserInformation { }
    public class CreatorUserInformation  : SentByUserInformation { }


    public class ProductInformation
    {
        public bool HasQuotation { get; set; }

        public int Quantity { get; set; }
        public int? QuantityUnit { get; set; }

        public string? Description { get; set; }
        public string? Justification { get; set; }

        public Guid PurchaseRequestId { get; set; }

        public ProductDetails ProductDetails { get; set; } = new ();
        public UnitMeasureInformation UnitMeasureInformation { get; set; } = new();
    }

    public class ProductDetails
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public CategoryInformation CategoryInformation { get; set; }= new ();
    } 

    public class CategoryInformation
    {
        public Guid CatagoryId { get; set;}
        public string? Name {get; set;}
        public string? Code {get; set;}
    }

    public class UnitMeasureInformation
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
    }
}
