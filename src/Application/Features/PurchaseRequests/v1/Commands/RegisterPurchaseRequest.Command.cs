using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands
{
    public class RegisterPurchaseRequestCommand : BaseRequest, IRequest<bool>
    {
        public Guid? AreaId { get; set; }
        public Guid BranchId { get; set; }
        public string? Observations { get; set; }

        public PriorityLevel? PriorityLevel { get; set; }
        public DestinationRequest Destination { get; set; }
        public PurchaseRequestType RequestType { get; set; }

        public List<PurchaseRequestItem> PurchaseRequestItems { get; set; } = [];
    }

    public class PurchaseRequestItem
    {
        public int Quantity { get; set; }
        public int? QuantityUnit { get; set; }

        public Guid ProductId { get; set; }
        public Guid UnitMeasureId { get; set; }

        public string? Description { get; set; }
        public string? Justification { get; set; }
        public string? AdditionalData { get; set; }
    }
}
