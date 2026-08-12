using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class RequisitionAccountingReviewDto
    {
        public Guid RequisitionAccountingReviewId { get; set; }
        public string? Comments { get; set; }
        public AccountingReviewStatus Status { get; set; }

        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new();

        public PurchaseRequestDetailsDto PurchaseRequest { get; set; } = new();
    }
}
