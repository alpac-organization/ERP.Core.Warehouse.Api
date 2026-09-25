using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterSectionCoordinateValidator : BaseRequestValidator<RegisterSectionCoordinateCommand>
    {
        public RegisterSectionCoordinateValidator()
        {
            RuleFor(x => x.WarehouseId).ValidateWarehouseId();

            RuleFor(x => x.PositionX)
               .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 2 decimales");

            RuleFor(x => x.PositionY)
               .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 2 decimales");

            RuleFor(x => x.PositionZ)
               .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 2 decimales");

            RuleFor(x => x.RotationY)
               .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360")
               .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("La rotación Y admite máximo 2 decimales");
        }
    }
}