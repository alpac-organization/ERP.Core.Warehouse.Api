using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Validators;

public class RegisterSupplyRequestsValidator : BaseRequestValidator<RegisterSupplyRequestsCommad>
{
    public RegisterSupplyRequestsValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("La justificación es obligatoria");

        RuleFor(x => x.OperationalOrderId)
            .NotEmpty().WithMessage("Debe especificar una Orden Operativa");

        RuleFor(x => x.ReasonUse)
            .IsInEnum().WithMessage("El motivo de uso especifico no es válido.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("La solicitud debe tener al menos un insumo.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductStockId).Distinct().Count() == items.Count)
            .When(x => x.Items is { Count: > 0 })
            .WithMessage("Hay insumos repetidos en la solicitud.");

        RuleForEach(x => x.Items)
            .SetValidator(new RegisterSupplyRequestItemValidator())
            .When(x => x.Items is { Count: > 0 });
    }
}

public class RegisterSupplyRequestItemValidator : AbstractValidator<RegisterSupplyRequestItem>
{
    public RegisterSupplyRequestItemValidator()
    {
        RuleFor(x => x.ProductStockId)
            .NotEmpty().WithMessage("El id del insumo es obligatorio.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
    }
}