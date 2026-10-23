using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Queries
{
    public class GetInventoryEntriesQuery : BaseRequest, IRequest<PagedResponse<InventoryEntryDto>>
    {
        public Guid? BranchId { get; set; }

        public EntryStatus? EntryStatus { get; set; }
        public string? PurchaseRequestCode { get; set; }

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}