using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands
{
    public class ProcessPurchaseOrderCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid RequisitionManagementReviewId { get; set; }
    }
}
