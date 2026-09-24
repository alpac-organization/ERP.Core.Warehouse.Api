using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetRacksBySectionValidator : BasePagedQueryValidator<GetRacksBySectionQuery>
{
    public GetRacksBySectionValidator() : base(100)
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("El estado del rack no es válido.");

        RuleFor(x => x.UsageProfile)
            .IsInEnum().When(x => x.UsageProfile.HasValue)
            .WithMessage("El perfil de uso del rack no es válido.");

        RuleFor(x => x.RowNumber)
            .GreaterThan(0).When(x => x.RowNumber.HasValue)
            .WithMessage("La hilera debe ser mayor a cero.");

        RuleFor(x => x.LevelNumber)
            .GreaterThan(0).When(x => x.LevelNumber.HasValue)
            .WithMessage("El nivel del rack debe ser mayor a cero.");
    }
}
