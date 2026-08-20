using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class PermanentDeleteEvidenceValidator : AbstractValidator<PermanentDeleteEvidenceCommand>
{
    public PermanentDeleteEvidenceValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de la compañía es obligatorio.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo es obligatorio.")
            .MaximumLength(50).WithMessage("El código del módulo no puede superar los 50 caracteres.");

        RuleFor(x => x.ReceptionId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de recepción es obligatorio.");
    }
}