using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries
{
    public class GetPurchaseRequestsQuery : BaseRequest, IRequest<PagedResponse<PurchaseRequestDto>>
    {
        public string? Code { get; set; }
        public Guid? BranchId { get; set; }
        public PurchaseRequestType? RequestType { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
