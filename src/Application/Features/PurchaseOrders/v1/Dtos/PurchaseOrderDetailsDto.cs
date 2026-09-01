using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

using ReviewerUserInformation = ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos.ReviewerUserInformation;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderDetailsDto : PurchaseOrderDto
    {
        public ReviewerUserInformation ReviewerUserInformation { get; set; } = new();
        public PurchaseRequestDetailsDto PurchaseRequestDetails { get; set; } = new();
    }
}
