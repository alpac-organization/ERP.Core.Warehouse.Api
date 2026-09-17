

using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
   public class RegisterSectionCoordinateValidator : AbstractValidator<RegisterSectionCoordinateCommand>
   {
      public RegisterSectionCoordinateValidator()
      {
         RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

         RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

         RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");

         RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El almacén es requerido.");

         RuleFor(x => x.PositionX)
            .NotEmpty().WithMessage("La coordenada en X es requerida")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 2 decimales");

         RuleFor(x => x.PositionY)
            .NotEmpty().WithMessage("La coordenada en Y es requerida")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 2 decimales");

         RuleFor(x => x.PositionZ)
            .NotEmpty().WithMessage("La coordenada en Z es requerida")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 2 decimales");

         RuleFor(x => x.RotationY)
            .NotEmpty().WithMessage("La rotación en Y es requerida")
            .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360");
      }
   }
}