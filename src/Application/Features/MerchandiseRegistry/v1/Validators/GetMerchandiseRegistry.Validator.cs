using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Validators;

public class GetMerchandiseRegistryValidator : AbstractValidator<GetMerchandiseRegistryQuery>
{
    public GetMerchandiseRegistryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la empresa es requerido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty()
            .WithMessage("El código del módulo es requerido.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("El número de página debe ser mayor a cero (0).");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("El tamaño de página debe ser mayor a cero (0).")
            .LessThanOrEqualTo(100)
            .WithMessage("El tamaño de página no puede exceder 100 registros.");
    }
}

public class GetMerchandiseRegistryDetailValidator : AbstractValidator<GetMerchandiseRegistryDetailsQuery>
{
    public GetMerchandiseRegistryDetailValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");
        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo es requerido.");
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");
        RuleFor(x => x.ReceptionId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de la recepción es obligatorio.");
    }

}