using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class PurchaseRequestsReviewedAccountingDto
    {
        public string? Comments { get; set; }
        public string? PurchaseRequestCode { get; set; }
        public AccountingReviewStatus Status { get; set; }
        public PurchaseRequestInformation? PurchaseRequestInformation { get; set; }
        
        public DateOnly SentToReviewAt { get; set; }
        public Guid PurchaseRequestsReviewedAccountingId { get; set; }
        public UserInformation SentByUserInformation { get; set; } = new();
    }

    public class PurchaseRequestInformation
    {
        public string? Code { get; set; }
        public Guid PurchaseRequestId { get; set; }
        public PriorityLevel PriorityLevel { get; set; }
        public PurchaseRequestType RequestType { get; set; }
    }
}
