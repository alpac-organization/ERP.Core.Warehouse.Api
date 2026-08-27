
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

using ReviewerUserInformation = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos.ReviewerUserInformation;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos
{
    public class RequisitionManagementReviewDetailsDto 
    {
         public string? Comments { get; set; }
        public DateOnly SentToReviewAt { get; set; }
        public ManagementReviewStatus Status { get; set; }
        public Guid RequisitionManagementReviewId { get; set; }

        public SentByUserInformation SentByUserInformation { get; set; } = new();
        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new();

        public PurchaseRequestDetailsDto PurchaseRequestDetails { get; set; } = new();
    }
}
