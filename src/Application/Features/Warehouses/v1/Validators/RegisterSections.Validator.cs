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

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código de la sección es obligatorio.")
                .MaximumLength(50).WithMessage("El código de la sección no puede superar los 50 caracteres.");

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
        }
    }
}
