namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
   public class LotCapacitiesDto
   {
      public Guid LotsId { get; set; }
      public decimal Width { get; set; }
      public decimal Length { get; set; }

      //M2
      public decimal TotalAreaM2 { get; set; }
      public decimal AvailableAreaWithMarginM2 { get; set; }
      public decimal UnusedAreaM2 { get; set; }
      public decimal UnoccupiedChargeableAreaM2 { get; set; }
      public decimal OccupiedChargeableAreaM2 { get; set; }

      public decimal PercentageAvailableAreaWithMarginM2 { get; set; }
   }
}