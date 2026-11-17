using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Validators;

public class RegisterEquipmentValidator : BaseRequestValidator<EquipmentCommand>
{
    public RegisterEquipmentValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("La sucursal es obligatoria.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca del equipo es obligatoria.")
            .MaximumLength(100).WithMessage("La marca no puede exceder los 100 caracteres.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código del equipo es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("El año del equipo es obligatorio")
            .MaximumLength(4).WithMessage("El año no puede exceder los 4 dígitos.")
            .Matches("^\\d{4}$").WithMessage("El año debe ser un año válido de 4 dígitos.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo del equipo es obligatorio.")
            .MaximumLength(100).WithMessage("El modelo no puede exceder los 100 caracteres.");

        RuleFor(x => x.SerialNumber)
            .NotEmpty().WithMessage("El número de serie del equipo es obligatorio.")
            .MaximumLength(100).WithMessage("El número de serie no puede exceder los 100 caracteres.");

        RuleFor(x => x.Color)
            .MaximumLength(100).WithMessage("El color no puede exceder los 100 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("El tipo de equipo no es válido.");

        RuleFor(x => x)
            .Must(x => x.Gasoline || x.Diesel || x.Electric || x.Lpg)
            .WithMessage("Debe enviarse al menos una bandera de combustible (gasoline, diesel, electric, lpg).");
    }
}