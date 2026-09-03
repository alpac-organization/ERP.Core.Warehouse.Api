using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos
{
    public class PurchaseRequestsReviewedManagementDetailsDto : PurchaseRequestsReviewedManagementDto
    {
        public UserInformation ReviewerUserInformation { get; set; } = new();
        public PurchaseRequestDetailsDto PurchaseRequestDetails { get; set; } = new();
    }
}
