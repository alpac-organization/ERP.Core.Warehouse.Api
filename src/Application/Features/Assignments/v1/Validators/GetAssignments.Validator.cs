using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class GetAssignmentsValidator : BaseRequestValidator<GetAssignmentsQuery>
    {
        public GetAssignmentsValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty().WithMessage("El ID de la orden operativa no es válido")
                .When(x => x.OperationalOrderId.HasValue);

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("El estado debe ser un valor de enum válido")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("El número de página debe ser mayor a 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a 0")
                .LessThanOrEqualTo(100).WithMessage("El tamaño de página no puede ser mayor a 100");
        }
    }
}