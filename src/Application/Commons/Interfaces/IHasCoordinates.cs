namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces
{
   public interface IHasCoordinates
   {
      decimal PositionX { get; }
      decimal PositionY { get; }
      decimal PositionZ { get; }
      decimal RotationY { get; }
   }
}