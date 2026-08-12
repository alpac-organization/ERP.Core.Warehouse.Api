using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries
{
    public class GetPurchaseRequestsProductsQuery : BaseRequest, IRequest<PagedResponse<PurchaseRequestItemDto>>
    {
        [JsonIgnore]
        public Guid PurchaseRequestId { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
