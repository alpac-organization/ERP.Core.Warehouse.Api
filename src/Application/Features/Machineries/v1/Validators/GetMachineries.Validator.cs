using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Validators;

public class GetMachineriesValidator : BaseRequestValidator<GetMachineriesQuery>
{
    public GetMachineriesValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue).WithMessage("el tipo de maquinaria no es válido.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero.");
    }
}