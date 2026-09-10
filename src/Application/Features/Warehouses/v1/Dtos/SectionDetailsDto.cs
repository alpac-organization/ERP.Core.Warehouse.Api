using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
   public class SectionDetailsDto
   {
      public Guid SectionId { get; set; }
      public string? SectionCode { get; set; }
      public bool IsActive { get; set; }
      public SectionCapacityDto Capacity { get; set; } = new();
      public SectionCoordinatesDto Coordinates { get; set; } = new();
   }

   public class SectionCapacityDto
   {
      public Guid SectionCapacityId { get; set; }

      public decimal Width { get; set; }

      public decimal Length { get; set; }

      public decimal UnusedAreaM2 { get; set; }

      public decimal AvailableAreaWithMarginM2 { get; set; }

      public decimal TotalAreaM2 { get; set; }

      public decimal UnoccupiedChargeableAreaM2 { get; set; }

      public decimal OccupiedChargeableAreaM2 { get; set; }

      public decimal PercentageAvailableAreaWithMarginM2 { get; set; }
   }

   public class SectionCoordinatesDto
   {
      public decimal PositionX { get; set; }
      public decimal PositionY { get; set; }
      public decimal PositionZ { get; set; }
   }
}
