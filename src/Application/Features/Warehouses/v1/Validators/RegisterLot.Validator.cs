using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterLotsCommandValidator : AbstractValidator<RegisterLotsCommand>
{
    public RegisterLotsCommandValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Groups).NotEmpty().WithMessage("Debe especificar al menos un grupo de tramos.");

        RuleForEach(x => x.Groups).ChildRules(group =>
        {
            group.RuleFor(g => g)
                .Must(g => (g.Codes is { Count: > 0 }) || (g.CodePrefix != null && g.StartNumber.HasValue && g.Count is > 0))
                .WithMessage("Debe indicar 'codes' o bien 'code_prefix' + 'start_number' + 'count'.");

            group.RuleFor(g => g.WidthMetres).GreaterThan(0);
            group.RuleFor(g => g.LengthMetres).GreaterThan(0);
            group.RuleFor(g => g.NominalRows).GreaterThan(0);
            group.RuleFor(g => g.NominalColumns).GreaterThan(0);
            group.RuleFor(g => g.Status).IsInEnum();

            group.RuleFor(g => g.UnavailableReason)
                .NotEmpty().MaximumLength(250)
                .When(g => g.Status is RackStatus.UnderMaintenance or RackStatus.Blocked);

            group.RuleFor(g => g.UnavailableReason)
                .Empty()
                .When(g => g.Status is RackStatus.Available or RackStatus.Occupied);
        });
    }
}