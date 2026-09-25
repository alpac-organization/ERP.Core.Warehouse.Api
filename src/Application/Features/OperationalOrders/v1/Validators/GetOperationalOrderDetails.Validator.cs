using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Validators
{
    public class GetOperationalOrderDetailsValidator : BaseRequestValidator<GetOperationalOrderDetailsQuery>
    {
        public GetOperationalOrderDetailsValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotNull()
                .WithMessage("El id de la orden operacional es obligatorio");
        }
    }
}