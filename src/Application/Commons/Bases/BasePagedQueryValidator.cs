using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BasePagedQueryValidator<TQuery> : BaseRequestValidator<TQuery>
    where TQuery : BaseRequest, IPagedQuery
{
    protected BasePagedQueryValidator(int maxPageSize = 10)
    {
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
            .LessThanOrEqualTo(maxPageSize)
            .WithMessage($"El tamaño de página no puede exceder {maxPageSize} registros.");
    }
}