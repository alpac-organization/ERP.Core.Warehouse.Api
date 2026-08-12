// Validators/RegisterRackCommandValidator.cs
using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterRacksBulkCommandValidator : AbstractValidator<RegisterRacksBulkCommand>
{
    public RegisterRacksBulkCommandValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.StartingDepositNumber).GreaterThan(0);
        RuleFor(x => x.Levels).NotEmpty().WithMessage("Debe especificar al menos un nivel.");

        RuleFor(x => x.Levels)
            .Must(levels => levels.Select(l => l.LevelNumber).Distinct().Count() == levels.Count)
            .When(x => x.Levels.Count > 0)
            .WithMessage("No puede repetir el mismo número de nivel.");

        RuleForEach(x => x.Levels).ChildRules(level =>
        {
            level.RuleFor(l => l.LevelNumber).GreaterThanOrEqualTo(0);
            level.RuleFor(l => l.RacksCount).GreaterThan(0)
                .WithMessage("La cantidad de racks por nivel debe ser mayor a 0.");
            level.RuleFor(l => l.WidthMetres).GreaterThan(0);
            level.RuleFor(l => l.LengthMetres).GreaterThan(0);
            level.RuleFor(l => l.HeightMetres).GreaterThan(0).When(l => l.HeightMetres.HasValue);
            level.RuleFor(l => l.MaxPulleys).GreaterThan(0);
            level.RuleFor(l => l.UsageProfile).IsInEnum();
            level.RuleFor(l => l.Status).IsInEnum();

            level.RuleFor(l => l.UnavailableReason)
                .NotEmpty().WithMessage("Debe indicar un motivo cuando el rack no está disponible.")
                .MaximumLength(250)
                .When(l => l.Status is RackStatus.UnderMaintenance or RackStatus.Blocked);

            level.RuleFor(l => l.UnavailableReason)
                .Empty().WithMessage("No debe indicar un motivo si el rack está disponible u ocupado.")
                .When(l => l.Status is RackStatus.Available or RackStatus.Occupied);
        });
    }
}