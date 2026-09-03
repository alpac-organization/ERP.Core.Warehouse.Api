using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class PurchaseRequestsReviewedAccountingDetailsDto : PurchaseRequestsReviewedAccountingDto
    {
        public Guid? ReviewedByUserId { get; set; }
        public PurchaseRequestDetailsDto PurchaseRequest { get; set; } = new();
    }
}
