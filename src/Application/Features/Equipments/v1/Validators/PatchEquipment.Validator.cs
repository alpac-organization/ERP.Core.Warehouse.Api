using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Validators;

public class PatchEquipmentValidator : BaseRequestValidator<PatchEquipmentCommand>
{
    public PatchEquipmentValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().When(x => x.BranchId.HasValue);

        RuleFor(x => x.Brand)
            .MaximumLength(100).When(x => x.Brand is not null);

        RuleFor(x => x.Code)
            .MaximumLength(50).When(x => x.Code is not null);

        RuleFor(x => x.Year)
            .MaximumLength(4).When(x => x.Year is not null)
            .Matches("^\\d{4}$").When(x => x.Year is not null);

        RuleFor(x => x.Model)
            .MaximumLength(100).When(x => x.Model is not null);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).When(x => x.SerialNumber is not null);

        RuleFor(x => x.Color)
            .MaximumLength(100).When(x => x.Color is not null);

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue);

        RuleFor(x => x.Type)
            .IsInEnum().When(x => x.Type.HasValue);

        RuleFor(x => x)
            .Must(x =>
                !(x.Gasoline.HasValue || x.Diesel.HasValue || x.Electric.HasValue || x.Lpg.HasValue) ||
                (x.Gasoline == true || x.Diesel == true || x.Electric == true || x.Lpg == true))
            .WithMessage("Si actualizas banderas de combustible, al menos una debe ser true.");
    }
}