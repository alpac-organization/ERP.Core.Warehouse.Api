using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterLotsValidator : BaseRequestValidator<RegisterLotsCommand>
{
    public RegisterLotsValidator()
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("La sección es obligatoria.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El almacén es obligatorio.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("La cantidad de tramos debe ser mayor que 0.")
            .LessThanOrEqualTo(10)
            .WithMessage($"Se permite un máximo de 10 tramos por petición.");

        RuleFor(x => x.NominalRows)
            .NotNull()
            .WithMessage("Las filas son obligatorias.")
            .GreaterThan(0)
            .WithMessage("Las filas deben ser mayor que 0.");

        RuleFor(x => x.NominalColumns)
            .NotNull()
            .WithMessage("Las columnas son obligatorias.")
            .GreaterThan(0)
            .WithMessage("Las columnas deben ser mayor que 0.");

        RuleFor(x => x.Width)
            .GreaterThan(0)
            .WithMessage("El ancho (metros) debe ser mayor que 0.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .WithMessage($"El ancho admite máximo 2 decimales.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("El largo (metros) debe ser mayor que 0.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .WithMessage($"El largo admite máximo 2 decimales.");
    }
}