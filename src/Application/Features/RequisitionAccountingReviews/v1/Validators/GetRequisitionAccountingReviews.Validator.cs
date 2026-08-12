using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Validators
{
    public class GetRequisitionAccountingReviewsValidator : AbstractValidator<GetRequisitionAccountingReviewsQuery>
    {
        public GetRequisitionAccountingReviewsValidator()
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
                .WithMessage("El estado de la revisión contable no es válido.");
        }
    }

    public class GetRequisitionAccountingReviewDetailsValidator : AbstractValidator<GetRequisitionAccountingReviewDetailsQuery>
    {
        public GetRequisitionAccountingReviewDetailsValidator()
        {
            RuleFor(x => x.CompanyId)
                .NotEqual(Guid.Empty).WithMessage("El Id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo es requerido.");

            RuleFor(x => x.UserId)
                .NotEqual(Guid.Empty).WithMessage("No se puede identificar al usuario.");

            RuleFor(x => x.RequisitionAccountingReviewId)
                .NotEqual(Guid.Empty).WithMessage("El identificador de la revisión contable es obligatorio.");
        }
    }
}
