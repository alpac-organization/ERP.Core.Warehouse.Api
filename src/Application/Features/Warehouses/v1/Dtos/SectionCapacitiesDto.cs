

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
   public class SectionCapacitiesDto
   {
      public Guid SectionId { get; set; }
      public decimal UsableAreaM2 { get; set; }
      public decimal? UnusableAreaM2 { get; set; }

      public decimal Width { get; set; }
      public decimal Length { get; set; }

      //M2
      public decimal? UnusedSpaceM2 { get; set; }
      public decimal AvailableSpaceWithSpacingM2 { get; set; }
      public decimal AvailableSpaceWithoutSpacingM2 { get; set; }

      public decimal PercenteAvailableSpaceWithSpacingM2 { get; set; }
      public decimal PercenteAvailableSpaceWithSpacingM3 { get; set; }

   }
}