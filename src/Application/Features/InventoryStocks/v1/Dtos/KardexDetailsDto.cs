using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos
{
    public class KardexDetailsDto : KardexDto
    {
        public decimal PreviousUnits { get; set; }
        public decimal PreviousKardex { get; set; }

        public UserInformation? MovementByUserInformation { get; set; }

        public Guid? InventoryOutputId { get; set; }
        public string? InventoryOutputCode { get; set; }
        public InventoryOutputsStatus? InventoryOutputStatus { get; set; }
        public string? ReasonRemovalDescription { get; set; }
        public string? OutputRequestCode { get; set; }
        public string? OutputRequestType { get; set; }
    }
}
