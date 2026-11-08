using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderReportDto : DocumentBase
    {
        public Guid PurchaseOrderId { get; set; }

        public string? PurchaseOrderCode { get; set; }

        public PaymentCondition PaymentCondition { get; set; }

        public string? PaymentRequestCode { get; set; }

        public PurchaseOrderReportPaymentInfo PaymentInfo { get; set; } = new();

        public PurchaseOrderReportDocumentInfo DocumentInfo { get; set; } = new();

        public UserInformation SentByUserInformation { get; set; } = new();

        public CompanyInformation CompanyInformation { get; set; } = new();

        public List<PurchaseOrderReportItemDto> Items { get; set; } = [];
    }

    public class PurchaseOrderReportPaymentInfo
    {
        public string? Supplier { get; set; }

        public string? Customer { get; set; }

        public string? Department { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal Iva { get; set; }

        public decimal IncomeTax { get; set; }

        public decimal NetToPay { get; set; }
    }

    public class PurchaseOrderReportDocumentInfo
    {
        public string? Title { get; set; }

        public string? RequestCode { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Date { get; set; }

        public bool IsNormal { get; set; }

        public bool IsCritical { get; set; }

        public bool IsUnforeseen { get; set; }
    }
}
