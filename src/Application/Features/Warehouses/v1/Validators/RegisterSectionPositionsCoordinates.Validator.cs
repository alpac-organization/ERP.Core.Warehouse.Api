using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterSectionPositionsCoordinatesValidator : BaseRequestValidator<RegisterSectionPositionsCoordinatesCommand>
{
    public RegisterSectionPositionsCoordinatesValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.PositionsInformation)
            .NotEmpty().WithMessage("Debe proporcionar al menos una posición de pasillo para registrar coordenadas.");

        RuleForEach(x => x.PositionsInformation).ChildRules(position =>
        {
            position.RuleFor(p => p.SectionPositionId)
                .NotEmpty().WithMessage("El id de la posición de pasillo es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El id de la posición de pasillo no es válido.");

            position.RuleFor(p => p.PositionX)
                .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 6 decimales");

            position.RuleFor(p => p.PositionY)
                .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 6 decimales");

            position.RuleFor(p => p.PositionZ)
                .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 6 decimales");

            position.RuleFor(p => p.RotationY)
                .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360")
                .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La rotación Y admite máximo 6 decimales");
        });
    }
}
