using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos
{
    public class KardexDetailsDto : KardexDto
    {
        public decimal PreviousUnits { get; set; }
        public decimal PreviousKardex { get; set; }

        public UserInformation? MovementByUserInformation { get; set; }

        public DateOnly? InvoiceDate { get; set; }
        public EntryStatus? InventoryEntryStatus { get; set; }
        public string? EntryObservations { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public string? SupplierName { get; set; }

        public InventoryOutputsStatus? InventoryOutputStatus { get; set; }
        public string? ReasonRemovalDescription { get; set; }
    }
}
