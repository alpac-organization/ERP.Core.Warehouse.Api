using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class PauseSessionValidator : BaseRequestValidator<PauseSessionCommand>
{
    public PauseSessionValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("La sesión de reasignamiento es requerida.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sesión es requerido.");
    }
}