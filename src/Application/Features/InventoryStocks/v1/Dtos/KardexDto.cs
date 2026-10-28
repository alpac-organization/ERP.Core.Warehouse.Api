using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos
{
    public class KardexDto
    {
        public Guid KardexId { get; set; }
        public KardexType KardexType { get; set; }
        public KardexMovementType MovementType { get; set; }
    }
}