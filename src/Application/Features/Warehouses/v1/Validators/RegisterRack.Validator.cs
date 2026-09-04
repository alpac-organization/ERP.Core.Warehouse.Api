using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterRackCommandValidator : AbstractValidator<RegisterRackCommand>
{
    public RegisterRackCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.SectionId).NotEmpty().WithMessage("La sección es obligatoria.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código del estante es obligatorio.")
            .MaximumLength(50);

        RuleFor(x => x.Levels)
            .NotEmpty().WithMessage("Debe especificar al menos un nivel.")
            .Must(levels => levels.Select(l => l.LevelNumber).Distinct().Count() == levels.Count)
            .WithMessage("Los números de nivel no pueden repetirse.");

        RuleForEach(x => x.Levels).ChildRules(level =>
        {
            level.RuleFor(l => l.LevelNumber).GreaterThan(0).WithMessage("El nivel debe ser mayor a 0.");
            level.RuleFor(l => l.WidthMetres).GreaterThan(0).WithMessage("El ancho debe ser mayor a 0.");
            level.RuleFor(l => l.LengthMetres).GreaterThan(0).WithMessage("El largo debe ser mayor a 0.");
            level.RuleFor(l => l.HeightMetres).GreaterThanOrEqualTo(0);
            level.RuleFor(l => l.MaxPulleys).GreaterThan(0).WithMessage("Máximo de polines debe ser mayor a 0.");
            level.RuleFor(l => l.UsageProfile).IsInEnum();
            level.RuleFor(l => l.Status).IsInEnum();

            level.RuleFor(l => l.UnavailableReason)
                .NotEmpty()
                .MaximumLength(250)
                .When(l => l.Status is RackStatus.UnderMaintenance or RackStatus.Blocked);

            level.RuleFor(l => l.UnavailableReason)
                .Empty()
                .When(l => l.Status is RackStatus.Available or RackStatus.Occupied);
        });

        // Todos los niveles deben compartir el mismo footprint (como en la UI)
        RuleFor(x => x.Levels)
            .Must(levels =>
            {
                if (levels.Count <= 1) return true;
                var first = levels[0];
                return levels.All(l => l.WidthMetres == first.WidthMetres && l.LengthMetres == first.LengthMetres);
            })
            .WithMessage("Todos los niveles deben tener el mismo ancho y largo.");

        When(x => x.LayoutTransform3DDto != null, () =>
        {
            RuleFor(x => x.LayoutTransform3DDto!)
                .Must(WarehouseLayoutValidation.HasValidNonNegativeCoordinates)
                .WithMessage("Las coordenadas X, Y y Z no pueden ser negativas.");

            RuleFor(x => x.LayoutTransform3DDto!.RotationY)
                .Must(WarehouseLayoutValidation.IsRightAngleRotation)
                .WithMessage("La rotación debe ser un ángulo recto (0, 90, 180, 270).");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var section = await unitOfWork.Sections.Entities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == command.SectionId && s.IsActive, cancellationToken);

                    if (section is null || command.Levels.Count == 0) return false;

                    var layout = command.LayoutTransform3DDto!;
                    var first = command.Levels[0];
                    var bounds = new WarehouseLayoutValidation.LayoutBounds(
                        layout.PositionX,
                        layout.PositionY,
                        layout.PositionZ,
                        layout.RotationY,
                        first.WidthMetres,
                        first.LengthMetres);

                    return WarehouseLayoutValidation.FitsWithinContainer(
                        bounds,
                        section.WidthMetres,
                        section.LengthMetres);
                })
                .WithMessage("El rack excede las dimensiones de la sección.");
        });
    }
}
