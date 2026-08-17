using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class ExitVehicleValidator : AbstractValidator<ExitVehicleCommand>
{
    public ExitVehicleValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.ReceptionId)
            .NotEqual(Guid.Empty).WithMessage("Debe indicar la recepción a la que se le registrará la salida.");

        RuleFor(x => x)
            .Must(x => x.ExitVehicle || x.ExitContainer)
            .WithMessage("Debe indicar al menos un tipo de salida (vehículo o contenedor).");

        When(x => x.ExitDate.HasValue, () =>
        {
            RuleFor(x => x.ExitDate!.Value)
                .LessThanOrEqualTo(NicaraguaClock.Today)
                .WithMessage("La fecha de salida no puede ser una fecha futura.");
        });

        RuleFor(x => x)
            .Must(x => x.ExitDate.HasValue || !x.ExitTime.HasValue)
            .WithMessage("Si especifica la hora de salida, también debe especificar la fecha de salida.");
    }
}