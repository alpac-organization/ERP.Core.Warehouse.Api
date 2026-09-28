using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands
{
    public class AnnulPurchaseRequestCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid PurchaseRequestId { get; set; }

        public string Reason { get; set; } = default!;
    }
}
