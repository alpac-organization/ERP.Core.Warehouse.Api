using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class GetAssignmentDetailsValidator : BaseRequestValidator<GetAssignmentDetailsQuery>
    {
        public GetAssignmentDetailsValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("El ID de la asignación es requerido");
        }
    }
}