using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Validators
{
    public class GetRequisitionManagementReviewsValidator : AbstractValidator<GetRequisitionManagementReviewsQuery>
    {
        public GetRequisitionManagementReviewsValidator()
        {
            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo es requerido.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El id de usuario es requerido.")
                .NotEqual(Guid.Empty).WithMessage("No se puede identificar al usuario.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("El número de página debe ser mayor que 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor que 0.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("El estado de la revisión de gerencia no es válido.");
        }
    }
}
