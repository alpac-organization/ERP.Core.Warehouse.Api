using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class PurchaseRequestDocumentDto : DocumentBase
    {
        public string? DocumentName { get; set; }
        public string? DocumentUrl { get; set; }
    }
}
