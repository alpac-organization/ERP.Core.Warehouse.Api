using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class DeleteAssignmentValidator : BaseRequestValidator<DeleteAssignmentCommand>
    {
        public DeleteAssignmentValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty()
                .WithMessage("El ID de la asignación es requerido");

            RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .WithMessage("El ID de la operación como tal es requerido");
        }
    }
}