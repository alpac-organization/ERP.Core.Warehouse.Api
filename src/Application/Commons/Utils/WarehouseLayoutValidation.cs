using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Utils
{
   public static class WarehouseLayoutValidation
   {
      public readonly record struct LayoutFootPrint(decimal WidthMetres, decimal LengthMetres);
      public readonly record struct LayoutBounds(
      decimal PositionX,
      decimal PositionY,
      decimal PositionZ,
      decimal RotationY,
      decimal WidthMetres,
      decimal LengthMetres);

      public static decimal NormalizeRotationY(decimal rotationY)
      {
         var normalized = rotationY % 360m;
         return normalized < 0 ? normalized + 360m : normalized;
      }

      public static LayoutFootPrint GetFootPrint(decimal widthMetres, decimal lengthMetres, decimal rotationY)
      {
         var normalizedRotation = NormalizeRotationY(rotationY);
         var isRotated = normalizedRotation is 90m or 270m;
         return isRotated
         ? new LayoutFootPrint(lengthMetres, widthMetres)
         : new LayoutFootPrint(widthMetres, lengthMetres);
      }
      public static LayoutFootPrint GetFootPrint(LayoutBounds bounds)
          => GetFootPrint(bounds.WidthMetres, bounds.LengthMetres, bounds.RotationY);

      public static bool FitsWithinContainer(
          decimal positionX,
          decimal positionZ,
          LayoutFootPrint footPrint,
          decimal containerWidthMetres,
          decimal containerLengthMetres
      )
      {
         if (containerWidthMetres <= 0 || containerLengthMetres <= 0) return false;

         var maxX = positionX + footPrint.WidthMetres;
         var maxY = positionZ + footPrint.LengthMetres;

         return positionX >= 0
                && positionZ >= 0
                && maxX <= containerWidthMetres
                && maxY <= containerLengthMetres;
      }
      public static bool FitsWithinContainer(
       LayoutBounds bounds,
       decimal containerWidthMetres,
       decimal containerLengthMetres
      )
      {
         var footprint = GetFootPrint(bounds);
         return FitsWithinContainer(
            bounds.PositionX,
            bounds.PositionZ,
            footprint,
            containerWidthMetres,
            containerLengthMetres
         );
      }

      public static bool HasValidNonNegativeCoordinates(LayoutTransform3DDto layout)
      => layout.PositionX >= 0
      && layout.PositionY >= 0
      && layout.PositionZ >= 0;
      public static bool IsRightAngleRotation(decimal rotationY)
      {
         var normalized = NormalizeRotationY(rotationY);
         return normalized is 0m or 90m or 180m or 270m;
      }
   }
}