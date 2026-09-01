using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries
{
    public class GetPurchaseOrdersQuery : BaseRequest, IRequest<PagedResponse<PurchaseOrderDto>>
    {
        //All filters here ...  
        public Guid? AreaId { get; set; }
        public Guid? BranchId { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}