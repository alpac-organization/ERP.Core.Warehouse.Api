using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Validators
{
    public class AcceptQuotationForPurchaseValidator : AbstractValidator<AcceptQuotationForPurchaseCommand>
    {
        public AcceptQuotationForPurchaseValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.QuotationId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la cotización no es válido.");

            RuleFor(x => x.PurchaseRequestItemId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador del producto solicitado no es válido.");
        }
    }
}
