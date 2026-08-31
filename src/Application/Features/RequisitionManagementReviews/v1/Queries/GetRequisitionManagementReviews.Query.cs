using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries
{
    public class GetRequisitionManagementReviewsQuery : BaseRequest, IRequest<PagedResponse<PurchaseRequestsReviewedManagementDto>>
    {
        public Guid? AreaId { get; set; }
        public Guid? BranchId { get; set; }
        public ManagementReviewStatus? Status { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
