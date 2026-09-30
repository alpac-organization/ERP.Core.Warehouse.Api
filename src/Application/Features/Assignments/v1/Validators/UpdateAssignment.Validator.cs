using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class UpdateAssignmentValidator : BaseRequestValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("El ID de la asignación es requerido");
        }
    }
}