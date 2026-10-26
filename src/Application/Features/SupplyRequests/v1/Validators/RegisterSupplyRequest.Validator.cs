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

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("La solicitud debe tener al menos un insumo.")
            .Must(items => items.Select(i => i.ProductStockId).Distinct().Count() == items.Count)
            .When(x => x.Items.Count > 0);

        RuleForEach(x => x.Items).SetValidator(new RegisterSupplyRequestItemValidator());
    }
}

public class RegisterSupplyRequestItemValidator : AbstractValidator<RegisterSupplyRequestItem>
{
    public RegisterSupplyRequestItemValidator()
    {
        RuleFor(x => x.ProductStockId)
            .NotEmpty().WithMessage("El id del producto es");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
    }
}