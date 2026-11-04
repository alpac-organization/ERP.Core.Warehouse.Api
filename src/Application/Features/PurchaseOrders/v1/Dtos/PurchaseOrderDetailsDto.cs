using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderDetailsDto : PurchaseOrderHeaderDto
    {
        public UserInformation ReviewerUserInformation { get; set; } = new();

        public PurchaseRequestDetailsDto PurchaseRequest { get; set; } = new();
    }
}
