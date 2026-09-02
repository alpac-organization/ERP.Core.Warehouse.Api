using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderDocumentTemplateDto : DocumentBase
    {
        public DocumentInfo DocumentInfo { get; set; } = new();
        public PaymentInfo PaymentInfo { get; set; } = new();
        public DocumentSignatureInfo Signatures { get; set; } = new();
    }
}
