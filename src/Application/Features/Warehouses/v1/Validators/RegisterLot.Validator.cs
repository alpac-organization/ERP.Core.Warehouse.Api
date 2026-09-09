using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterLotCommandValidator : BaseRequestValidator<RegisterLotCommand>
{
    public RegisterLotCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("La sección es obligatoria.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El almacén es obligatorio.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("El código del tramo es obligatorio.");

        RuleFor(x => x.WidthMetres)
            .GreaterThan(0)
            .WithMessage("El ancho (metros) debe ser mayor que 0.");

        RuleFor(x => x.LengthMetres)
            .GreaterThan(0)
            .WithMessage("El largo (metros) debe ser mayor que 0.");

        RuleFor(x => x.NominalRows)
            .GreaterThan(0)
            .WithMessage("Las filas nominales deben ser mayores que 0.");

        RuleFor(x => x.NominalColumns)
            .GreaterThan(0)
            .WithMessage("Las columnas nominales deben ser mayores que 0.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("El estado del tramo no es válido.");
    }
}