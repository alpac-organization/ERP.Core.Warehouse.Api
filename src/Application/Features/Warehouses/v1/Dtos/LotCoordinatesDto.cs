namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
   public class LotCoordinatesDto
   {
      public Guid LotId { get; set; }
      public decimal PositionX { get; set; }
      public decimal PositionY { get; set; }
      public decimal PositionZ { get; set; }
      public decimal RotationY { get; set; }
   }
}