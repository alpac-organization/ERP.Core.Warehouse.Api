using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Validators;

public class RegisterOperationalServiceValidator : BaseRequestValidator<RegisterOperationalServicesCommand>
{
    public RegisterOperationalServiceValidator()
    {
        RuleFor(x => x.ServiceName)
            .NotEmpty().WithMessage("El nombre del servicio es requerido")
            .MaximumLength(200).WithMessage("El nombre de servicio no puede exceder los 200 caracteres.");
    
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithMessage("El código del servicio es requerido")
            .MaximumLength(50).WithMessage("El código de servicio no puede exceder los 50 caracteres.");
    
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del servicio es requerido")
            .MaximumLength(500).WithMessage("La descripción de servicio no puede exceder los 500 caracteres.");
    }
}