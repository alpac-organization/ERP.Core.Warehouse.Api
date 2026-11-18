using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos
{
    public class InventoryEntryDto
    {
        public Guid InventoryEntryId { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? Code { get; set; }
        public string? PurchaseRequestCode { get; set; }
    
        public EntryType EntryType { get; set; }
        public EntryStatus EntryStatus { get; set; }

        public BranchInformation? BranchInformation { get; set; }
        public WorkAreaInformation? WorkAreaInformation { get; set; }
    }
}