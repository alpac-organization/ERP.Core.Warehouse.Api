using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands
{
    public class ProcessPurchaseRequestCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid PurchaseRequestId { get; set; }
        public string? ReasonRejection { get; set; }
        public PurchaseRequestStatus NewStatus { get; set; }
    }
}
