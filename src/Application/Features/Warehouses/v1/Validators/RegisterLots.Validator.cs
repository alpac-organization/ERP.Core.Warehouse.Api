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

        RuleFor(x => x.Lots)
            .NotEmpty()
            .WithMessage("Debe enviar al menos un tramo.")
            .Must(lots => lots.Count <= 10)
            .WithMessage("Se permite un máximo de 10 tramos por petición.");

        RuleForEach(x => x.Lots).SetValidator(new RegisterLotItemValidator());
    }
}

public class RegisterLotItemValidator : AbstractValidator<RegisterLotItem>
{
    public RegisterLotItemValidator()
    {
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
            .NotNull()
            .WithMessage("El ancho (metros) es obligatorio.")
            .GreaterThan(0)
            .WithMessage("El ancho (metros) debe ser mayor que 0.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .WithMessage("El ancho admite máximo 2 decimales.");

        RuleFor(x => x.Length)
            .NotNull()
            .WithMessage("El largo (metros) es obligatorio.")
            .GreaterThan(0)
            .WithMessage("El largo (metros) debe ser mayor que 0.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .WithMessage("El largo admite máximo 2 decimales.");

        RuleFor(x => x.PositionX)
            .NotNull().WithMessage("La coordenada X es requerida.")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 6 decimales");

        RuleFor(x => x.PositionY)
            .NotNull().WithMessage("La coordenada Y es requerida.")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 6 decimales");

        RuleFor(x => x.PositionZ)
            .NotNull().WithMessage("La coordenada Z es requerida.")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 6 decimales");

        RuleFor(x => x.RotationY)
            .NotNull().WithMessage("La rotación en Y es requerida.")
            .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360")
            .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La rotación Y admite máximo 6 decimales");
    }
}
