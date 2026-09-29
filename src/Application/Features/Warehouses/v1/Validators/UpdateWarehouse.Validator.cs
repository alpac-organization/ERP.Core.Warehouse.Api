using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class UpdateWarehouseValidator : BaseRequestValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");

        RuleFor(x => x)
            .Must(x =>
                x.Code is not null ||
                x.IsActive.HasValue ||
                x.WarehouseType.HasValue ||
                x.Location is not null ||
                x.Capacity is not null)
            .WithMessage("Debe enviar al menos un campo para actualizar.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código del almacén no puede estar vacío.")
            .MaximumLength(20).WithMessage("El código del almacén no puede superar los 20 caracteres.")
            .When(x => x.Code is not null);

        RuleFor(x => x.WarehouseType)
            .IsInEnum().WithMessage("El tipo de almacén no es válido.")
            .When(x => x.WarehouseType.HasValue);

        When(x => x.Location is not null, () =>
        {
            RuleFor(x => x.Location!.LocationName)
                .NotEmpty().WithMessage("El nombre de la ubicación no puede estar vacío.")
                .When(x => x.Location!.LocationName is not null);
        });

        When(x => x.Capacity is not null, () =>
        {
            RuleFor(x => x.Capacity!.Width)
                .GreaterThan(0).WithMessage("El ancho debe ser mayor a cero.")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.Width.HasValue);

            RuleFor(x => x.Capacity!.Length)
                .GreaterThan(0).WithMessage("El largo debe ser mayor a cero.")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.Length.HasValue);

            RuleFor(x => x.Capacity!.MaximumHeight)
                .GreaterThan(0).WithMessage("La altura máxima debe ser mayor a cero.")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.MaximumHeight.HasValue);

            RuleFor(x => x.Capacity!.MinimumHeight)
                .GreaterThanOrEqualTo(0).WithMessage("La altura mínima no puede ser negativa.")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.MinimumHeight.HasValue);

            RuleFor(x => x.Capacity!)
                .Must(c =>
                    !c.MinimumHeight.HasValue ||
                    !c.MaximumHeight.HasValue ||
                    c.MaximumHeight >= c.MinimumHeight)
                .WithMessage("La altura máxima debe ser mayor o igual a la altura mínima.");

            RuleFor(x => x.Capacity!.MarginTop)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.MarginTop.HasValue);

            RuleFor(x => x.Capacity!.MarginBottom)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.MarginBottom.HasValue);

            RuleFor(x => x.Capacity!.MarginLeft)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.MarginLeft.HasValue);

            RuleFor(x => x.Capacity!.MarginRight)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .When(x => x.Capacity!.MarginRight.HasValue);
        });
    }
}
