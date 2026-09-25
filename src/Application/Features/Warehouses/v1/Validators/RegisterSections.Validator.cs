using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterSectionValidator : BaseRequestValidator<RegisterSectionCommand>
    {
        public RegisterSectionValidator()
        {
            RuleFor(x => x.WarehouseId).ValidateWarehouseId();

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código de la sección es obligatorio.")
                .MaximumLength(50).WithMessage("El código de la sección no puede superar los 50 caracteres.");

            RuleFor(x => x.SectionType)
                .IsInEnum().WithMessage("El tipo de sección no es válido.");

            RuleFor(x => x.SectionStorageType)
                .IsInEnum().WithMessage("El tipo de almacenaje para sección no es válido.");

            RuleFor(x => x.Width).ValidateDimension("ancho");

            RuleFor(x => x.Length).ValidateDimension("largo");
        }
    }
}
