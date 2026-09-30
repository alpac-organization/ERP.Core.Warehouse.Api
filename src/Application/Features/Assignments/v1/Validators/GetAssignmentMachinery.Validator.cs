
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators;    

public class GetAssignmentMachineryValidator : BaseRequestValidator<GetAssignmentMachineryQuery>
{
    public GetAssignmentMachineryValidator()
    {
        RuleFor(x => x.AssignmentId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la asignacion operativa es requerida");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero.");
    }
}