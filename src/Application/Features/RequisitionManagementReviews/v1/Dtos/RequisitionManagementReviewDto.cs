using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos
{
    public class RequisitionManagementReviewDto
    {
        public string? Comments { get; set; }
        public DateOnly SentToReviewAt { get; set; }
        public ManagementReviewStatus Status { get; set; }

        public Guid RequisitionManagementReviewId { get; set; }

        public PurchaseRequestDto PurchaseRequest { get; set; } = new();
        public SentByUserInformation SentByUserInformation { get; set; } = new();
    }
}
