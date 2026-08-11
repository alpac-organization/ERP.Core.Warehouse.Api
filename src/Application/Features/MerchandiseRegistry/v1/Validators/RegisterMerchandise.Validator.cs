using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Validators;

public class RegisterMerchandiseValidator : AbstractValidator<RegisterMerchandiseCommand>
{
     public RegisterMerchandiseValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El id de usuario es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de usuario no es válido");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("El id de la empresa es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la empresa no es válido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código de módulo es requerido.");

        RuleFor(x => x.MerchandiseName)
            .NotEmpty().WithMessage("El nombre de la mercadería es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre de la mercadería no puede exceder los 200 caracteres.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .NotEqual(Guid.Empty).WithMessage("El id de la categoría no es válido.");
    }
}