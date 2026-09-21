using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class AnnulPurchaseRequestValidator : BaseRequestValidator<AnnulPurchaseRequestCommand>
    {
        public AnnulPurchaseRequestValidator()
        {
            RuleFor(x => x.PurchaseRequestId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la solicitud de compra no es válido.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("El motivo de anulación es obligatorio.")
                .MaximumLength(1000)
                .WithMessage("El motivo de anulación no puede exceder los 1000 caracteres.");
        }
    }
}
