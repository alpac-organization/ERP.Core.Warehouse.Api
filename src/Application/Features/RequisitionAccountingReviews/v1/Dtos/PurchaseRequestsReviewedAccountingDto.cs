using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class PurchaseRequestsReviewedAccountingDto
    {
        public string? Comments { get; set; }
        public DateOnly SentToReviewAt { get; set; }
        public Guid PurchaseRequestsReviewedAccountingId { get; set; }
        public AccountingReviewStatus Status { get; set; }
        
        public UserInformation SentByUserInformation { get; set; } = new();
    }
}
