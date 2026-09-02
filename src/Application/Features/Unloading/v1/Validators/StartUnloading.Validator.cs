using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Validators;

public class StartUnloadingValidator : BaseRequestValidator<StartUnloadingCommand>
{
    public StartUnloadingValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("El identificador de la asignación no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de la asignación no es válido.");

        RuleFor(x => x.ProcessedByUserName)
            .NotEmpty().WithMessage("El nombre del usuario que inicia la descarga es obligatorio.")
            .MaximumLength(450).WithMessage("El nombre del usuario no puede superar los 450 caracteres.");

        RuleFor(x => x.MerchandiseType)
            .IsInEnum().WithMessage("El tipo de mercadería no es válido.");

        RuleFor(x => x.Pallets)
            .NotEmpty().WithMessage("Debe declarar al menos un polín para iniciar la descarga.");

        RuleForEach(x => x.Pallets).SetValidator(new StartUnloadingPalletItemValidator());

        RuleFor(x => x.Supplies)
            .NotEmpty().WithMessage("Debe declarar al menos un insumo para iniciar la descarga.");

        RuleForEach(x => x.Supplies).SetValidator(new StartUnloadingSupplyItemValidator());
    }
}

public class StartUnloadingPalletItemValidator : AbstractValidator<StartUnloadingPalletItem>
{
    public StartUnloadingPalletItemValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("El tipo de polín no es válido.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad de polines debe ser mayor a cero (0).");

        RuleFor(x => x.LenghtMetres)
            .NotNull().When(x => x.Type == PalletType.Oversized)
            .WithMessage("El largo del polín es obligatorio para polines sobredimensionados.")
            .GreaterThan(0).When(x => x.LenghtMetres.HasValue)
            .WithMessage("El largo del polín debe ser mayor a cero (0).");

        RuleFor(x => x.WidthMetres)
            .NotNull().When(x => x.Type == PalletType.Oversized)
            .WithMessage("El ancho del polín es obligatorio para polines sobredimensionados.")
            .GreaterThan(0).When(x => x.WidthMetres.HasValue)
            .WithMessage("El ancho del polín debe ser mayor a cero (0).");
    }
}

public class StartUnloadingSupplyItemValidator : AbstractValidator<StartUnloadingSupplyItem>
{
    public StartUnloadingSupplyItemValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del insumo es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción del insumo no puede superar los 500 caracteres.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad del insumo debe ser mayor a cero (0).");
    }
}
