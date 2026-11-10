using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries
{
    public class GetPurchaseOrderReportQuery : BaseRequest, IRequest<PurchaseOrderReportDto>
    {
        public Guid PurchaseOrderId { get; set; }
    }
}
