using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderHeaderDto
    {
        public string? Comments { get; set; }

        public DateOnly SentToReviewAt { get; set; }

        public Guid PurchaseOrderId { get; set; }

        public string? PurchaseOrderCode { get; set; }

        public string? PaymentRequestCode { get; set; }

        public Guid? SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public UserInformation SentByUserInformation { get; set; } = new();
    }

    public class PurchaseOrderDto : PurchaseOrderHeaderDto
    {
        public PurchaseRequestSummaryDto PurchaseRequest { get; set; } = new();
    }
}
