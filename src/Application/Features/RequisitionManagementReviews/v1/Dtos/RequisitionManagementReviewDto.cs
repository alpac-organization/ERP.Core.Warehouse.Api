using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos
{
    public class RequisitionManagementReviewDto
    {
        public string? Comments { get; set; }
        public DateOnly SentToReviewAt { get; set; }
        public ManagementReviewStatus Status { get; set; }
        public Guid RequisitionManagementReviewId { get; set; }
        public string? AccountingReviewComments { get; set; }
        public SentByUserInformation SentByUserInformation { get; set; } = new();
    }

    public class SentByUserInformation
    {
        public Guid UserId { get; set; }
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? PictureUrl { get; set; }
        public UserStatus UserStatus { get; set; }
    }
}
