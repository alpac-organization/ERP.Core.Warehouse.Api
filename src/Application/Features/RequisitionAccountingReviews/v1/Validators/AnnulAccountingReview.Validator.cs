using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Validators
{
    public class AnnulAccountingReviewValidator : AbstractValidator<AnnulAccountingReviewCommand>
    {
        public AnnulAccountingReviewValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.RequisitionAccountingReviewId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la revisión contable no es válido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.Scope)
                .IsInEnum()
                .WithMessage("El alcance de la anulación no es válido.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("El motivo de anulación es obligatorio.")
                .MaximumLength(1000)
                .WithMessage("El motivo de anulación no puede exceder los 1000 caracteres.");
        }
    }
}
