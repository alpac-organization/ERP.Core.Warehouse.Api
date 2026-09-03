using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestDetailsDto : PurchaseRequestDto
    {
        public string? Observations { get; set; }
        public string? ReasonRejection { get; set; }

        public UserInformation  CreatorUserInformation { get; set; } = new();
        public UserInformation? ReviewerUserInformation { get; set; } = null;
        public BranchInformation BranchInformation { get; set; } = new ();
        public WorkAreaInformation InformationFromRequestingArea { get; set; } = new ();
    }
}
