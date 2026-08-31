using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries
{
    public class GetRequisitionAccountingReviewsQuery : BaseRequest, IRequest<PagedResponse<RequisitionAccountingReviewDto>>
    {
        public Guid? AreaId { get; set; }
        public Guid? BranchId { get; set; }
        public AccountingReviewStatus? Status { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
