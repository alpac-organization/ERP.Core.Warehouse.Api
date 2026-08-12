using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class RequisitionAccountingReviewDto
    {
        public string? Comments { get; set; }
        public DateOnly SentToReviewAt { get; set; }
        public AccountingReviewStatus Status { get; set; }
        public Guid RequisitionAccountingReviewId { get; set; }
        public SentByUserInformation SentByUserInformation { get; set; } = new();
    }

    public class SentByUserInformation
    {
        public Guid UserId { get; set; }
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? PictureUrl { get; set; }
        public UserStatus UserStatus { get; set; }
        public WorkAreaInformation WorkAreaInformation { get; set; } = new();
    }

    public class ReviewerUserInformation : SentByUserInformation { }
}
