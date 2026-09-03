using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    /// <summary>
    /// Indica el template pdf - html
    /// </summary>
    public class PurchaseOrderTemplateDto : DocumentBase
    {
        public PaymentInfo PaymentInfo { get; set; } = new();
        public DocumentInfo DocumentInfo { get; set; } = new();
        public UserInformation SentByUserInformation { get; set; } = new();
        public CompanyInformation CompanyInformation { get; set; } = new();
    }
}
