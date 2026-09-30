using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterSectionValidator : BaseRequestValidator<RegisterSectionCommand>
    {
        public RegisterSectionValidator()
        {
            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("El almacén es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El almacén es requerido.");

            RuleFor(x => x.SectionType)
                .IsInEnum().WithMessage("El tipo de sección no es válido.");

            RuleFor(x => x.SectionStorageType)
                .IsInEnum().WithMessage("El tipo de almacenaje para sección no es válido.");

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("El ancho debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El ancho admite máximo 2 decimales");

            RuleFor(x => x.Length)
                .GreaterThan(0).WithMessage("El largo debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El largo admite máximo 2 decimales");

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
