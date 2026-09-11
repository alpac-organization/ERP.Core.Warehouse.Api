using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class UpdateLotValidator : BaseRequestValidator<UpdateLotCommand>
{
    public UpdateLotValidator()
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("La sección es obligatoria.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El almacén es obligatorio.");

        RuleFor(x => x.LotId)
            .NotEmpty()
            .WithMessage("El tramo es obligatorio.");

        RuleFor(x => x.Code)
            .MaximumLength(50)
            .When(x => x.Code != null)
            .WithMessage("El código del tramo no puede superar los 50 caracteres.");

        RuleFor(x => x.WidthMetres)
            .GreaterThan(0)
            .When(x => x.WidthMetres.HasValue)
            .WithMessage("El ancho (metros) debe ser mayor que 0.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.WidthMetres.HasValue)
            .WithMessage("El ancho admite máximo 2 decimales.");

        RuleFor(x => x.LengthMetres)
            .GreaterThan(0)
            .When(x => x.LengthMetres.HasValue)
            .WithMessage("El largo (metros) debe ser mayor que 0.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.LengthMetres.HasValue)
            .WithMessage("El largo admite máximo 2 decimales.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("El estado del tramo no es válido.");
    }
}