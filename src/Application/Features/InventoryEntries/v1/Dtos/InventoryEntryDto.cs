using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos
{
    public class InventoryEntryDto
    {
        public Guid InventoryEntryId { get; set; }
        public string? Code { get; set; }
        public EntryStatus EntryStatus { get; set; }
        public BranchInformation? BranchInformation { get; set; }
    }
}