using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;
 
namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands
{
public class UpdatePurchaseCommand : BaseRequest, IRequest<bool>
{
   [JsonIgnore]
   public Guid PurchaseRequestId {get; set;}

   public string? Observations {get; set;}

   public PriorityLevel? PriorityLevel {get; set;}

   public DestinationRequest? DestinationRequest {get; set;}

   public List<UpdatePurchaseRequestItem>? PurchaseRequestItems {get; set;} = []; 
}
    public class UpdatePurchaseRequestItem
    {
        public Guid? Id { get; set; }

        public int? Quantity { get; set; }
        public int? QuantityUnit { get; set; }

        public Guid? ProductId { get; set; }
        public Guid? UnitMeasureId { get; set; }

        public string? Description { get; set; }
        public string? Justification { get; set; }

        public List<string>? ImagesProductToChanged {get; set;}
    }
}