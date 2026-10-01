namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
   /// <summary>
   /// Datos completos para dibujar el plano 2D de una seccion: su forma real y
   /// todos sus tramos con dimensiones y coordenadas.
   /// </summary>
   public class LotLayoutDto
   {
      public Guid SectionId { get; set; }
      public string? SectionCode { get; set; }

      public bool SectionIsActive { get; set; }

      /// <summary>Ancho de la seccion en metros, 0 si no tiene capacidad registrada.</summary>
      public decimal SectionWidth { get; set; }

      /// <summary>Largo de la seccion en metros, 0 si no tiene capacidad registrada.</summary>
      public decimal SectionLength { get; set; }

      /// <summary>Posicion de la seccion dentro de la bodega, en metros.</summary>
      public decimal SectionPositionX { get; set; }

      /// <summary>Posicion de la seccion dentro de la bodega, en metros.</summary>
      public decimal SectionPositionY { get; set; }
      public List<LotLayoutItemDto> Lots { get; set; } = [];
   }

   public class LotLayoutItemDto
   {
      public Guid LotId { get; set; }
      public string Code { get; set; } = string.Empty;

      /// <summary>Ancho del tramo en metros, 0 si no tiene capacidad registrada.</summary>
      public decimal Width { get; set; }

      /// <summary>Largo del tramo en metros, 0 si no tiene capacidad registrada.</summary>
      public decimal Length { get; set; }

      /// <summary>
      /// Indica si el tramo ya tiene coordenadas persistidas. Determina si el
      /// guardado debe usar el endpoint de registro o el de actualizacion.
      /// </summary>
      public bool HasCoordinates { get; set; }
      public decimal PositionX { get; set; }
      public decimal PositionY { get; set; }
      public decimal PositionZ { get; set; }
      public decimal RotationY { get; set; }
   }
}
