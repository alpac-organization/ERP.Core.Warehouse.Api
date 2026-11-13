using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Queries
{
    public class GetInventoryEntryDetails : BaseRequest, IRequest<InventoryEntryDetailDto>
    {
        public Guid InventoryEntryId { get; set; }
        public PurchaseRequestDto? PurchaseRequestInformation { get; set; }
    }
}