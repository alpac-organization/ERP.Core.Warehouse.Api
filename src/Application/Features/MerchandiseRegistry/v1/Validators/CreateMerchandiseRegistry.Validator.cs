using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Validators;

public class CreateDucatRegistryValidator : AbstractValidator<CreateDucatRegistryCommand>
{
    public CreateDucatRegistryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.ReceptionId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de recepción es obligatorio.");

        RuleFor(x => x.ContainerNumber)
            .NotEmpty().WithMessage("El número de contenedor es obligatorio.")
            .MaximumLength(50).WithMessage("El número de contenedor no puede superar los 50 caracteres.");

        RuleFor(x => x.Empresa)
            .NotEmpty().WithMessage("La naviera/empresa es obligatoria.")
            .MaximumLength(150).WithMessage("La naviera/empresa no puede superar los 150 caracteres.");

        RuleFor(x => x.GeneralObservations)
            .MaximumLength(1000).WithMessage("Las observaciones generales no pueden superar los 1000 caracteres.");

        RuleFor(x => x.RegisteredStartDate)
            .NotNull().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.RegisteredStartTime)
            .NotNull().WithMessage("La hora de inicio es obligatoria.");
    }
}