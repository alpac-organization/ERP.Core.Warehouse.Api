
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators;

public class DeleteAssignmentCollaboratorsValidator : BaseRequestValidator<DeleteAssignmentCollaboratorsCommand>
{
    public DeleteAssignmentCollaboratorsValidator()
    {
        RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("La orden operativa es requerida");

        RuleFor(x => x.AssignmentId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la asignacion operativa es requerido");

        RuleFor(x => x.AssignmentCollaboratorId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("El id del colaborador asignado es requerido");
    }
}
