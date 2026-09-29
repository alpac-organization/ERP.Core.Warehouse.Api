using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class UpdateSectionLayoutValidator : BaseRequestValidator<UpdateSectionLayoutCommand>
    {
        public UpdateSectionLayoutValidator()
        {
            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("El almacén es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El almacén es requerido.");

            RuleFor(x => x.SectionId)
                .NotEmpty().WithMessage("La sección es requerida.")
                .NotEqual(Guid.Empty).WithMessage("La sección es requerida.");

            RuleFor(x => x)
                .Must(x => x.PositionX.HasValue || x.PositionY.HasValue || x.Width.HasValue || x.Length.HasValue)
                .WithMessage("Debe enviar al menos un campo para actualizar (position_x, position_y, width o length).");

            RuleFor(x => x.PositionX)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 2 decimales")
                .When(x => x.PositionX.HasValue);

            RuleFor(x => x.PositionY)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 2 decimales")
                .When(x => x.PositionY.HasValue);

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("El ancho debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El ancho admite máximo 2 decimales")
                .When(x => x.Width.HasValue);

            RuleFor(x => x.Length)
                .GreaterThan(0).WithMessage("El largo debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El largo admite máximo 2 decimales")
                .When(x => x.Length.HasValue);
        }
    }
}
