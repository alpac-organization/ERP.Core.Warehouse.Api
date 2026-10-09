using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
using ERP.Core.Warehouse.Api.Application.Commons.Services;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderTemplateDto : DocumentBase
    {
        public Guid PurchaseOrderId { get; set; }

        public string? PurchaseOrderCode { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        public string? PaymentRequestCode { get; set; }

        public PurchaseOrderTaxMetadata? Tax { get; set; }

        public PaymentInfo PaymentInfo { get; set; } = new();

        public DocumentInfo DocumentInfo { get; set; } = new();

        public UserInformation SentByUserInformation { get; set; } = new();
        public CompanyInformation CompanyInformation { get; set; } = new();

        public List<PurchaseOrderReportItemDto> Items { get; set; } = [];
    }

    public class PurchaseOrderReportItemDto
    {
        public Guid PurchaseOrderItemId { get; set; }

        public Guid? PurchaseRequestItemId { get; set; }

        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? ProductCode { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal PriceTotal { get; set; }

        public decimal Iva { get; set; }
    }
}
