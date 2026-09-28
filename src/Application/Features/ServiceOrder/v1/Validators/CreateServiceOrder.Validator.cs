using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Validators
{
    public class CreateServiceOrderValidator : BaseRequestValidator<CreateServiceOrderCommand>
    {
        public CreateServiceOrderValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .WithMessage("La po padre debe ser designada");

            RuleFor(x => x.OperationalServiceId)
                .NotEmpty()
                .WithMessage("El servicio a designar es obligatorio");
        }
    }
}