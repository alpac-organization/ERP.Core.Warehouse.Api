using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestDetailsDto : PurchaseRequestDto
    {
        public string? Observations { get; set; }
        public string? ReasonRejection { get; set; }

        public BranchInformation BranchInformation { get; set; } = new ();
        public CreatorUserInformation  CreatorUserInformation { get; set; } = new();
        public ReviewerUserInformation? ReviewerUserInformation { get; set; } = null;
        public WorkAreaInformation  InformationFromRequestingArea { get; set; } = new ();
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
}
