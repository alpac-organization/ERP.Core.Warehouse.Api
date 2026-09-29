using MediatR;
using ERP.Core.Database.Domain.Enums;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands
{
    public class ProcessPurchaseOrderCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid RequisitionManagementReviewId { get; set; }

        public ManagementReviewStatus NewStatus { get; set; }

        public string? Comments { get; set; }
    }
}
