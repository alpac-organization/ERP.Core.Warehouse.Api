using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Validators;

public class ApproveSupplyRequestValidator : BaseRequestValidator<ApproveSupplyRequestCommand>
{
    public ApproveSupplyRequestValidator()
    {
        RuleFor(x => x.SupplyRequestId)
            .NotEqual(Guid.Empty)
            .WithMessage("El identificador de la solicitud de insumos no es válido.");

        RuleFor(x => x.ReasonInventoryRemovalId)
            .NotEqual(Guid.Empty)
            .WithMessage("El motivo de salida de inventario es obligatorio.");

        RuleFor(x => x.Observations)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Observations))
            .WithMessage("Las observaciones no pueden exceder los 1000 caracteres.");
    }
}
