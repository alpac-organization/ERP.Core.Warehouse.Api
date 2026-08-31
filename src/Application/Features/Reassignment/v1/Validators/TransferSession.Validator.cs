using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class TransferSessionValidator : BaseRequestValidator<TransferSessionCommand>
{
    public TransferSessionValidator()
    {
        RuleFor(c => c.SessionId)
            .NotEmpty().WithMessage("La sessión de reasignamiento es requerida.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sesión es requerido.");

        RuleFor(c => c.NewOwnerUserId)
            .NotEmpty().WithMessage("El nuev dueño de la sesiónes requerido.");
    }
}