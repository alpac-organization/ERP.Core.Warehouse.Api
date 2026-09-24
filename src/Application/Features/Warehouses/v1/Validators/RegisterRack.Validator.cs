using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterRacksBulkCommandValidator : AbstractValidator<RegisterRacksBulkCommand>
{
    public RegisterRacksBulkCommandValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad de racks debe ser mayor a cero.")
            .LessThanOrEqualTo(60).WithMessage("No se pueden registrar más de 60 racks en una sola operación.");

        RuleFor(x => x.RowNumber)
            .GreaterThan(0).WithMessage("El número de hilera debe ser mayor a cero.");

        RuleFor(x => x.LevelNumber)
            .InclusiveBetween(1, 10).WithMessage("El número de niveles debe estar entre 1 y 10.");

        RuleFor(x => x.MaxPulleys)
            .InclusiveBetween(1, 10).WithMessage("El máximo de polines por nivel debe estar entre 1 y 10.");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("El ancho del rack debe ser mayor a cero.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("El largo del rack debe ser mayor a cero.");

        RuleFor(x => x.Height)
            .GreaterThan(0).When(x => x.Height.HasValue)
            .WithMessage("La altura del rack debe ser mayor a cero.");

        RuleFor(x => x.UsageProfile)
            .IsInEnum().WithMessage("El perfil de uso no es válido.");

        RuleFor(x => x.InitialPositionX)
            .GreaterThanOrEqualTo(0).WithMessage("La coordenada inicial X debe ser mayor o igual a cero.");

        RuleFor(x => x.InitialPositionY)
            .GreaterThanOrEqualTo(0).WithMessage("La coordenada inicial Y debe ser mayor o igual a cero.");

        RuleFor(x => x.SpacingX)
            .GreaterThan(0).WithMessage("La separación horizontal (spacing_x) debe ser mayor a cero.");
    }
}
