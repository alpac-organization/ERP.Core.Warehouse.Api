using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries
{
    public class GetRequisitionAccountingReviewDetailsQuery : BaseRequest, IRequest<PurchaseRequestsReviewedAccountingDetailsDto>
    {
        public Guid RequisitionAccountingReviewId { get; set; }
    }
}
