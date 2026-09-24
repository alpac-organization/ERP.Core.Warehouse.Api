using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class UpdateSectionValidator : BaseRequestValidator<UpdateSectionCommand>
    {
        public UpdateSectionValidator()
        {
            RuleFor(x => x.WarehouseId).ValidateWarehouseId();

            RuleFor(x => x.SectionId)
                .NotEmpty().WithMessage("La sección es requerida.")
                .NotEqual(Guid.Empty).WithMessage("La sección es requerida.");

            RuleFor(x => x)
                .Must(x => x.Code is not null || x.Width.HasValue || x.Length.HasValue)
                .WithMessage("Debe enviar al menos un campo para actualizar.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código de la sección no puede estar vacío.")
                .MaximumLength(50).WithMessage("El código de la sección no puede superar los 50 caracteres.")
                .When(x => x.Code is not null);

            RuleFor(x => x.Width!.Value)
                .ValidateDimension("ancho")
                .When(x => x.Width.HasValue);

            RuleFor(x => x.Length!.Value)
                .ValidateDimension("largo")
                .When(x => x.Length.HasValue);
        }
    }
}
