
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

using ReviewerUserInformation = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos.ReviewerUserInformation;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos
{
    public class PurchaseRequestsReviewedManagementDetailsDto : PurchaseRequestsReviewedManagementDto
    {
        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new();
        public PurchaseRequestDetailsDto PurchaseRequestDetails { get; set; } = new();
    }
}
