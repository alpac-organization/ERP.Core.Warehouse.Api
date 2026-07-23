using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class AddDucatsToReceptionValidator : AbstractValidator<AddDucatsToReceptionCommand>
{
    public AddDucatsToReceptionValidator()
    {
         RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la empresa es requerido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty()
            .WithMessage("El código del módulo es requerido.");

        RuleFor(x => x.ReceptionId)
            .NotEqual(Guid.Empty)
            .WithMessage("El identificador de la recepción es obligatorio.");

        RuleFor(x => x.DucatNumbers)
            .NotEmpty()
            .WithMessage("Debe incluir al menos un número de DUCA.");

        RuleForEach(x => x.DucatNumbers)
            .NotEmpty()
            .WithMessage("El número de DUCA no puede estar vacío.")
            .MaximumLength(100)
            .WithMessage("El número de DUCA no puede exceder los 100 caracteres.");
    }
}