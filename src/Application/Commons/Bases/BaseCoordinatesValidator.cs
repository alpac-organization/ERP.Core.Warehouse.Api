using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases
{
   public class BaseCoordinatesValidator<T> : AbstractValidator<T> where T : IHasCoordinates
   {
      public BaseCoordinatesValidator()
      {
         RuleFor(x => x.PositionX)
            .NotNull().WithMessage("La coordenada X es requerida.")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 6 decimales");

         RuleFor(x => x.PositionY)
            .NotNull().WithMessage("La coordenada Y es requerida.")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 6 decimales");

         RuleFor(x => x.PositionZ)
            .NotNull().WithMessage("La coordenada Z es requerida.")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 6 decimales");

         RuleFor(x => x.RotationY)
            .NotNull().WithMessage("La rotación en Y es requerida.")
            .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La rotación Y admite máximo 6 decimales");
      }
   }
}
