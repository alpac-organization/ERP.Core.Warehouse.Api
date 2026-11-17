using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Validators;

public class GetEquipmentsValidator : BaseRequestValidator<GetEquipmentsQuery>
{
    public GetEquipmentsValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue).WithMessage("el tipo de equipo no es válido.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero.");
    }
}