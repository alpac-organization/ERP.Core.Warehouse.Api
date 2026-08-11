using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Validators;

public class GetMerchandisesValidator : AbstractValidator<GetMerchandisesQuery>
{
    public GetMerchandisesValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("El id de la empresa es requerido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo es requerido.");
    }
}