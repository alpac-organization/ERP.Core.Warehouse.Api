using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos
{
    public class KardexDto
    {
        public Guid KardexId { get; set; }
        public KardexType KardexType { get; set; }
        public KardexMovementType MovementType { get; set; }

        public decimal Units { get; set; }
        public decimal TotalUnits { get; set; }
        
        public decimal UnitCost { get; set; }
        public decimal TotalInput { get; set; }
        public decimal TotalOutput { get; set; }
        public decimal AverageCost { get; set; }
        public decimal TotalKardex { get; set; }

        public ProductInformation? ProductInformation { get; set; }
        public UnitMeasureInformation? UnitMeasureInformation { get; set; }
    }
}