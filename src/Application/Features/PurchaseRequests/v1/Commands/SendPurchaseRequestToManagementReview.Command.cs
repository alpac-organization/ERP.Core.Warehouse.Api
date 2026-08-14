using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands
{
    public class SendPurchaseRequestToManagementReviewCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid PurchaseRequestId { get; set; }
        public string? Comments { get; set; }
    }
}
