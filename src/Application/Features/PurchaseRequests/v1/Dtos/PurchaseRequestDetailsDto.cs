using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestDetailsDto : PurchaseRequestDto
    {
        public string? Observations { get; set; }
        public string? ReasonRejection { get; set; }
        public string? AdditionalData { get; set; }

        public bool IsManagementApproved { get; set; }
        public bool IsAccountingApproved { get; set; }
        public bool IsPurchaseOrderGenerated { get; set; }

        public UserInformation  CreatorUserInformation { get; set; } = new();
        public UserInformation? ReviewerUserInformation { get; set; } = null;
        public BranchInformation BranchInformation { get; set; } = new ();
        public WorkAreaInformation InformationFromRequestingArea { get; set; } = new ();
        public CostCenterInformation CostCenterInformation { get; set; } = new();
        
        public List<PurchaseRequestItemDto> PurchaseRequestItems { get; set; } = [];
    }
}
