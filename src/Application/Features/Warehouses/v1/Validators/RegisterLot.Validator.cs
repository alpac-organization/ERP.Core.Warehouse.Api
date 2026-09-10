using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterLotValidator : BaseRequestValidator<RegisterLotCommand>
{
    public RegisterLotValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("La sección es obligatoria.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El almacén es obligatorio.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código del tramo es obligatorio.")
            .MaximumLength(50)
            .WithMessage("El código del tramo no puede superar los 50 caracteres.");

        RuleFor(x => x.WidthMetres)
            .GreaterThan(0)
            .WithMessage("El ancho (metros) debe ser mayor que 0.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithMessage("El ancho admite máximo 2 decimales.");

        RuleFor(x => x.LengthMetres)
            .GreaterThan(0)
            .WithMessage("El largo (metros) debe ser mayor que 0.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithMessage("El largo admite máximo 2 decimales.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("El estado del tramo no es válido.");
    }
}