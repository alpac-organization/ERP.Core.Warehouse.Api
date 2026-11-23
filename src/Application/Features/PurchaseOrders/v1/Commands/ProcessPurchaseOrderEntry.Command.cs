using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands
{
    public class ProcessPurchaseOrderEntryCommand : BaseRequest, IRequest<Unit>
    {

        [JsonIgnore]
        public Guid PurchaseOrderId { get; set; }
        public string? Observations { get; set; }
    }
}
