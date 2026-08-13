using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos
{
    public class RequisitionAccountingReviewDetailsDto : RequisitionAccountingReviewDto
    {
        public Guid? ReviewedByUserId { get; set; }
        public PurchaseRequestDetailsDto PurchaseRequest { get; set; } = new();
    }

    public class WorkAreaInformation
    {
        public Guid WorkAreaId { get; set; }
        public int WorkAreaCode { get; set; }
        public string? Description { get; set; }
        public string? WorkAreaName { get; set; }
        
        public List<CostCenterInformation>? CostCenters { get; set; } = [];
    }

    public class CostCenterInformation
    {
        public Guid CostCenterId { get; set; }
        public string? Description { get; set; }
        public string? CostCenterName { get; set; }
        public int CoilCode { get; set; }
        public int CostCenterCode { get; set; }
    }
}
