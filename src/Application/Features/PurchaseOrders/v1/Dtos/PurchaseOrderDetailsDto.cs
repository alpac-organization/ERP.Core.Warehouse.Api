using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderDetailsDto : PurchaseOrderHeaderDto
    {
        public UserInformation ReviewerUserInformation { get; set; } = new();

        public List<PurchaseOrderDetailItemDto> Items { get; set; } = [];
    }

    public class PurchaseOrderDetailItemDto
    {
        public Guid PurchaseOrderItemId { get; set; }

        public Guid? PurchaseRequestItemId { get; set; }

        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? ProductCode { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public PurchaseOrderDetailQuotationDto? Quotation { get; set; }
    }

    public class PurchaseOrderDetailQuotationDto
    {
        public Guid QuotationId { get; set; }

        public bool IsAcceptedForPurchase { get; set; }

        public decimal PriceUnit { get; set; }

        public decimal PriceTotal { get; set; }

        public PaymentMethodType PaymentMethodType { get; set; }

        public Guid SupplierId { get; set; }

        public SupplierInformation SupplierInformation { get; set; } = new();
    }
}
