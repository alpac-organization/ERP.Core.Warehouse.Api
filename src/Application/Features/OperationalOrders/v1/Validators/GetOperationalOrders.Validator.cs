using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Validators
{
    public class GetOperationalOrdersValidator : BaseRequestValidator<GetOperationalOrdersQuery>
    {
        public GetOperationalOrdersValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("El estado debe ser un valor de enum valido")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.CustomerCif)
                .NotEmpty()
                .WithMessage("El código de cliente no debe ser vacío.")
                .When(x => x.CustomerCif is not null);

            RuleFor(x => x.PoCode)
                .NotEmpty()
                .WithMessage("El código de la PO de cliente no debe ser vacío.")
                .When(x => x.PoCode is not null);
        }
    }
}
