using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos
{
    public class PurchaseOrderDocumentDto: DocumentBase
    {
        public string? DocumentName { get; set; }
        public string? DocumentUrl { get; set; }
        
    }
}
