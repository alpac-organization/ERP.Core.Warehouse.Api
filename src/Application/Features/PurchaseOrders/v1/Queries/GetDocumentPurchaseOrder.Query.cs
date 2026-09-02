using MediatR;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries
{
    public class GetDocumentPurchaseOrderQuery : BaseRequest, IRequest<PurchaseOrderDocumentDto>
    {
        public Guid PurchaseOrderId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
    }
}
