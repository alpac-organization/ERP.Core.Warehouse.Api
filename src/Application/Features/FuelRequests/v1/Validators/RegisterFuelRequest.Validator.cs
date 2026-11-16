using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Validators;

public class RegisterFuelRequestsValidator : BaseRequestValidator<RegisterFuelRequestsCommand>
{
    public RegisterFuelRequestsValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("La justificación es obligatoria");

        RuleFor(x => x.MachineryId)
            .NotEmpty().WithMessage("Debe especificar la maquinaria/vehículo");

        RuleFor(x => x.ProductStockId)
            .NotEmpty().WithMessage("Debe especificar el stock de combustible");

        RuleFor(x => x.RequestedQuantity)
            .GreaterThan(0).WithMessage("La cantidad solicitada debe ser mayor que cero");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Mileage.HasValue)
            .WithMessage("El kilometraje no puede ser negativo.");

        RuleFor(x => x.EngineHours)
            .GreaterThanOrEqualTo(0)
            .When(x => x.EngineHours.HasValue)
            .WithMessage("Las horas motor no pueden ser negativas.");

        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .When(x => x.CustomerId.HasValue)
            .WithMessage("El cliente no es valido.");
    }
}