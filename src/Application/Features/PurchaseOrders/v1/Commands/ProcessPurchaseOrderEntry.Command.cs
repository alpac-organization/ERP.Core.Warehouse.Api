using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands
{
    public class ProcessPurchaseOrderEntryCommand : BaseRequest, IRequest<InventoryEntryDto>
    {
        public Guid PurchaseOrderId { get; set; }
        public string? Comments { get; set; }
    }
}
