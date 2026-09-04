using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetRacksBySectionValidator : BasePagedQueryValidator<GetRacksBySectionQuery>
{
    public GetRacksBySectionValidator() : base(100)
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("El estado del rack no es válido.");

        RuleFor(x => x.UsageProfile)
            .IsInEnum()
            .When(x => x.UsageProfile.HasValue)
            .WithMessage("El perfil de uso del rack no es válido.");

        RuleFor(x => x.LevelNumber)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LevelNumber.HasValue)
            .WithMessage("El nivel del rack no es válido.");

        RuleFor(x => x.WidthMetres)
            .GreaterThan(0)
            .When(x => x.WidthMetres.HasValue)
            .WithMessage("El ancho del rack debe ser mayor a cero.");

        RuleFor(x => x.LengthMetres)
            .GreaterThan(0)
            .When(x => x.LengthMetres.HasValue)
            .WithMessage("El largo del rack debe ser mayor a cero.");
    }
}
