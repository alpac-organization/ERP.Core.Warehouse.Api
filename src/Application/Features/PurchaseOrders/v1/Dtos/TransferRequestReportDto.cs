using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class TransferRequestReportDto : DocumentBase
    {
        public Guid PurchaseOrderId { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string? PaymentRequestCode { get; set; }

        public TransferRequestPaymentInfo PaymentInfo { get; set; } = new();

        public TransferRequestDocumentInfo DocumentInfo { get; set; } = new();

        public UserInformation SentByUserInformation { get; set; } = new();

        public CompanyInformation CompanyInformation { get; set; } = new();

        public List<PurchaseOrderReportItemDto> Items { get; set; } = [];
    }

    public class TransferRequestPaymentInfo
    {
        public string? Supplier { get; set; }

        public string? Customer { get; set; }

        public string? Department { get; set; }

        public Currency Currency { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal Iva { get; set; }

        public decimal Ir { get; set; }

        public decimal Imi { get; set; }

        public decimal NetToPay { get; set; }
    }

    public class TransferRequestDocumentInfo
    {
        public string? Title { get; set; }

        public string? RequestCode { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Date { get; set; }
    }
}
