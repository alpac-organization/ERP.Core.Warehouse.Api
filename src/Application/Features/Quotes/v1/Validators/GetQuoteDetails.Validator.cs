using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Validators
{
    public class GetQuoteDetailsValidator : AbstractValidator<GetQuoteDetailsQuery>
    {
        public GetQuoteDetailsValidator()
        {
            RuleFor(x => x.CompanyId)
                .NotEmpty()
                    .WithMessage("El id de la empresa no puedes vacio.")
                .NotNull()
                    .WithMessage("El id de la empresa es requerido");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                    .WithMessage("El codigo de modulo es requerido")
                .NotNull()
                    .WithMessage("El codigo de modulo es requerido");

            RuleFor(x => x.UserId)
                .NotEmpty()
                    .WithMessage("El id de usuario es requerido")
                .NotNull()
                    .WithMessage("El id de usuario es requerido");

            RuleFor(x => x.QuotationId)
                .NotEmpty()
                .WithMessage("El id de la cotización es requerido.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la cotización no es válido.");
        }
    }
}

