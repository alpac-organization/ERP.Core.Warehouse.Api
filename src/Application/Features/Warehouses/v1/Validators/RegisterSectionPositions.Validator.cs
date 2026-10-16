using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterSectionPositionsValidator : BaseRequestValidator<RegisterSectionPositionsCommand>
{
    public RegisterSectionPositionsValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.Rows)
            .GreaterThan(0).WithMessage("Las filas deben ser mayores a cero.");

        RuleFor(x => x.Columns)
            .GreaterThan(0).WithMessage("Las columnas deben ser mayores a cero.");

        RuleFor(x => x.Levels)
            .GreaterThan(0).WithMessage("Los niveles deben ser mayores a cero.");
    }
}
