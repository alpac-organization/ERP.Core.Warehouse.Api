using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries
{
    public class GetPurchaseRequestDocumentQuery : BaseRequest, IRequest<PurchaseRequestDocumentDto>
    {
        public Guid? PurchaseRequestId { get; set; }
        public PurchaseRequestType DocumentType { get; set; }
        public PurchaseRequestConsolidationType ConsolidationType { get; set; } = PurchaseRequestConsolidationType.ByArea;
        public int? Month { get; set; }
        public int? Year { get; set; }
    }
}
