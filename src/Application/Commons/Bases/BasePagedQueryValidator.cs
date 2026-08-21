using FluentValidation;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BasePagedQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : BaseRequest, IPagedQuery
{
    protected BasePagedQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("El id de la empresa no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido");

        RuleFor(x => x.ModuleCode)
            .NotEmpty()
            .WithMessage("El codigo de modulo es requerido.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El id de usuario es requerido.")
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x)
            .Must(x => ((long)x.PageNumber - 1) * x.PageSize <= int.MaxValue)
            .When(x => x.PageNumber > 0 && x.PageSize > 0)
            .WithMessage("La combinación de número de página y tamaño de página produce un desplazamiento inválido.")
            .WithName("PageNumber");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("El número de página debe ser mayor a cero (0).");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("El tamaño de página debe ser mayor a cero (0).")
            .LessThanOrEqualTo(10)
            .WithMessage("El tamaño de página no puede exceder 10 registros.");
    }
}