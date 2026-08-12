using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class SendPurchaseRequestToReviewValidator : AbstractValidator<SendPurchaseRequestToReviewCommand>
    {
        public SendPurchaseRequestToReviewValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.PurchaseRequestId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la solicitud de compra no es válido.");
                
            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");
        }
    }
}
