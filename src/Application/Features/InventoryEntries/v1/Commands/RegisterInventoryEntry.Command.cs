using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands
{
    public class RegisterInventoryEntryCommand : BaseRequest, IRequest<Unit>
    {
        public EntryType EntryType { get; set; }
        public string? Observations { get; set; }
        public Guid? InventoryOutputId { get; set; }


        public List<string> EvidenceImages { get; set; } = [];
        public List<InventoryEntryItems> EntryItems { get; set; } = []; 
    }

    public class InventoryEntryItems
    {
        public int Amount { get; set; }
        public Guid ProductStockId { get; set; }
    }
}