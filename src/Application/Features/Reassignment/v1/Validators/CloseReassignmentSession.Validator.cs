using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class CloseReassignmentSessionValidator : BaseRequestValidator<CloseReassignmentSessionCommand>
{
    public CloseReassignmentSessionValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("La sesión de reasignamiento es requerida.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sesión es requerido.");
    }
}
