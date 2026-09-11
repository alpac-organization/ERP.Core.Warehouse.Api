using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class UpdateSectionValidator : AbstractValidator<UpdateSectionCommand>
    {
        public UpdateSectionValidator()
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

            RuleFor(x => x.SectionId)
                .NotEmpty().WithMessage("La sección es requerida.")
                .NotEqual(Guid.Empty).WithMessage("La sección es requerida.");

            RuleFor(x => x)
                .Must(x => x.Code is not null || x.SectionType.HasValue || x.SectionStorageType.HasValue || x.Width.HasValue || x.Length.HasValue || x.IsActive.HasValue)
                .WithMessage("Debe enviar al menos un campo para actualizar.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código de la sección no puede estar vacío.")
                .MaximumLength(50).WithMessage("El código de la sección no puede superar los 50 caracteres.")
                .When(x => x.Code is not null);

            RuleFor(x => x.SectionType)
                .IsInEnum().WithMessage("El tipo de sección no es válido.")
                .When(x => x.SectionType.HasValue);

            RuleFor(x => x.SectionStorageType)
                .IsInEnum().WithMessage("El tipo de almacenaje para secciones no es válido.")
                .When(x => x.SectionStorageType.HasValue);

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("El ancho debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El ancho admite máximo 2 decimales")
                .When(x => x.Width.HasValue);

            RuleFor(x => x.Length)
                .GreaterThan(0).WithMessage("El largo debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El largo admite máximo 2 decimales")
                .When(x => x.Length.HasValue);
        }
    }
}
