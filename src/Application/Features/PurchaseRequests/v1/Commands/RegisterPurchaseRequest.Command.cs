using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands
{
    public class RegisterPurchaseRequestCommand : BaseRequest, IRequest<bool>
    {
        public List<RegisterPurchaseRequest> PurchaseRequests { get; set; } = [];
    }

    public class RegisterPurchaseRequest
    {
        public Guid? AreaId { get; set; }
        public Guid BranchId { get; set; }
        public Guid CostCenterId { get; set; }
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

        public Guid? ProductId { get; set; }
        public Guid? UnitMeasureId { get; set; }

        public List<Guid>? AdditionalSupplierIds { get; set; }

        public string? Description { get; set; }
        public string? Justification { get; set; }
        public string? AdditionalData { get; set; }

        public NewProductPayload? NewProduct { get; set; }
    }

    public class NewProductPayload
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UnitMeasureId { get; set; }
        public ProductUsageType ProductUsageType { get; set; }
        public bool IsTaxExempt { get; set; }

        public List<NewProductSupplierLink> Suppliers { get; set; } = [];
    }

    public class NewProductSupplierLink
    {
        public Guid SupplierId { get; set; }

        public decimal? UnitPrice { get; set; }
    }
}
