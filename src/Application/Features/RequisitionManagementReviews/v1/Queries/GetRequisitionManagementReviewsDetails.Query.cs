using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries
{
    public class GetRequisitionManagementReviewsDetailsQuery : BaseRequest, IRequest<RequisitionManagementReviewDetailsDto>
    {
        public Guid? RequisitionManagementReviewsId { get; set; }
        
    }
}
