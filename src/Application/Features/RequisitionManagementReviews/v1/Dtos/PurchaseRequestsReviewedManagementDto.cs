using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos
{
    public class PurchaseRequestsReviewedManagementHeaderDto
    {
        public string? Comments { get; set; }

        public DateOnly SentToReviewAt { get; set; }

        public ManagementReviewStatus Status { get; set; }

        public Guid PurchaseRequestsReviewedManagementId { get; set; }

        public UserInformation SentByUserInformation { get; set; } = new();
    }

    public class PurchaseRequestsReviewedManagementDto : PurchaseRequestsReviewedManagementHeaderDto
    {
        public PurchaseRequestSummaryDto PurchaseRequest { get; set; } = new();
    }
}
