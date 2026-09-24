using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class UpdateRackValidator : BaseRequestValidator<UpdateRackCommand>
{
    public UpdateRackValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El almacén es obligatorio.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("La sección es obligatoria.");

        RuleFor(x => x.RackId)
            .NotEmpty().WithMessage("El rack es obligatorio.");

        RuleFor(x => x.RowNumber)
            .GreaterThan(0)
            .When(x => x.RowNumber.HasValue)
            .WithMessage("El número de hilera debe ser mayor a 0.");

        RuleFor(x => x.Width)
            .GreaterThan(0)
            .When(x => x.Width.HasValue)
            .WithMessage("El ancho debe ser mayor que 0.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .When(x => x.Length.HasValue)
            .WithMessage("El largo debe ser mayor que 0.");

        RuleFor(x => x.Height)
            .GreaterThan(0)
            .When(x => x.Height.HasValue)
            .WithMessage("La altura debe ser mayor que 0.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("El estado del rack no es válido.");

        RuleFor(x => x.UsageProfile)
            .IsInEnum()
            .When(x => x.UsageProfile.HasValue)
            .WithMessage("El perfil de uso del rack no es válido.");
    }
}
