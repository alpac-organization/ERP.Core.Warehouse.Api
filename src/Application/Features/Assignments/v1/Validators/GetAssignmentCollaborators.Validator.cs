
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators;

public class GetAssignmentCollaboratorsValidator : BaseRequestValidator<GetAssignmentCollaboratorsQuery>
{
    public GetAssignmentCollaboratorsValidator()
    {
        RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("La orden operativa es requerida");

        RuleFor(x => x.AssignmentId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la asignacion operativa es requerido");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero.");
    }
}
