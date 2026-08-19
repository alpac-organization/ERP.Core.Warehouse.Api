using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Validators;

public class CreateShippingCompanyValidator : AbstractValidator<RegisterShippingCompanyCommand>
{
    public CreateShippingCompanyValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de la compañía es obligatorio.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo es obligatorio.")
            .MaximumLength(50).WithMessage("El código del módulo no puede superar los 50 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la naviera es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre de la naviera no puede superar los 150 caracteres.")
            .Must(name => !string.IsNullOrWhiteSpace(name.SanitizeAlphanumeric()))
            .WithMessage("El nombre de la naviera debe contener caracteres válidos.");
    }
}