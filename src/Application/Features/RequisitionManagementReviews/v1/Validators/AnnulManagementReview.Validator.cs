using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Validators
{
    public class AnnulManagementReviewValidator : BaseRequestValidator<AnnulManagementReviewCommand>
    {
        public AnnulManagementReviewValidator()
        {
            RuleFor(x => x.RequisitionManagementReviewId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la revisión de gerencia no es válido.");

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
