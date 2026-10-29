using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Validators;

public class RegisterCustomerValidator : BaseRequestValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(c => c.LegalName)
            .NotEmpty().WithMessage("El nombre del Cliente es requerido.")
            .MaximumLength(250).WithMessage("El nombre de cliente no puede superar los 250 caracteres.");
        
        RuleFor(c => c.IdentificationNumber)
            .NotEmpty().WithMessage("El número de identificación es requerido.")
            .MaximumLength(20).WithMessage("El número de identificación no puede superar los 20 caracteres.");

        RuleFor(c => c.CustomerType)
            .IsInEnum().WithMessage("El tipo de cliente no es válido.");

        RuleFor(c => c.IdentificationType)        
            .IsInEnum().WithMessage("El tipo de identificación no es válido.");
    }
}