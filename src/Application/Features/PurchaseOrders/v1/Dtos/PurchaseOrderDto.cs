using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderDto
    {
        public string? Comments { get; set; }
        public DateOnly SentToReviewAt { get; set; }
        
        public Guid PurchaseOrderId { get; set; }

        public UserInformation SentByUserInformation { get; set; } = new();
        public PurchaseRequestDetailsDto PurchaseRequest { get; set; } = new();
    }
}
