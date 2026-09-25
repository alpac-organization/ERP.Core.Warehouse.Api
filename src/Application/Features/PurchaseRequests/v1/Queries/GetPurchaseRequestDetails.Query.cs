using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries
{
    public class GetPurchaseRequestDetailsQuery : BaseRequest, IRequest<PurchaseRequestDetailsDto>
    {
        public Guid PurchaseRequestId { get; set; }
    }
}
