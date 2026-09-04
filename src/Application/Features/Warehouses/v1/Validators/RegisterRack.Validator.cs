using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterRacksBulkCommandValidator : AbstractValidator<RegisterRacksBulkCommand>
{
    public RegisterRacksBulkCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.SectionId).NotEmpty().WithMessage("La sección es obligatoria.");

        RuleFor(x => x.PlacementsRacks)
            .NotEmpty().WithMessage("Debe especificar al menos un rack.");

        RuleForEach(x => x.PlacementsRacks).ChildRules(placement =>
        {
            placement.RuleFor(p => p.Code)
                .NotEmpty().WithMessage("El código del estante es obligatorio.")
                .MaximumLength(50);

            placement.RuleFor(p => p.Levels)
                .NotEmpty().WithMessage("Debe especificar al menos un nivel.")
                .Must(levels => levels.Select(l => l.LevelNumber).Distinct().Count() == levels.Count)
                .WithMessage("Los números de nivel no pueden repetirse.");

            placement.RuleFor(p => p.Levels)
                .Must(levels =>
                {
                    if (levels.Count <= 1) return true;
                    var first = levels[0];
                    return levels.All(l => l.WidthMetres == first.WidthMetres && l.LengthMetres == first.LengthMetres);
                })
                .WithMessage("Todos los niveles deben tener el mismo ancho y largo.");

            placement.RuleForEach(p => p.Levels).ChildRules(level =>
            {
                level.RuleFor(l => l.LevelNumber).GreaterThan(0).WithMessage("El nivel debe ser mayor a 0.");
                level.RuleFor(l => l.WidthMetres).GreaterThan(0).WithMessage("El ancho debe ser mayor a 0.");
                level.RuleFor(l => l.LengthMetres).GreaterThan(0).WithMessage("El largo debe ser mayor a 0.");
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

            placement.When(p => p.LayoutTransform3DDto != null, () =>
            {
                placement.RuleFor(p => p.LayoutTransform3DDto!)
                    .Must(WarehouseLayoutValidation.HasValidNonNegativeCoordinates)
                    .WithMessage("Las coordenadas X, Y y Z no pueden ser negativas.");

                placement.RuleFor(p => p.LayoutTransform3DDto!.RotationY)
                    .Must(WarehouseLayoutValidation.IsRightAngleRotation)
                    .WithMessage("La rotación debe ser un ángulo recto (0, 90, 180, 270).");
            });
        });

        RuleFor(x => x.PlacementsRacks)
            .CustomAsync(async (placements, context, cancellationToken) =>
            {
                var command = (RegisterRacksBulkCommand)context.InstanceToValidate;

                var section = await unitOfWork.Sections.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == command.SectionId && s.IsActive, cancellationToken);

                if (section is null)
                {
                    context.AddFailure("SectionId", "La sección asignada no existe o está inactiva.");
                    return;
                }

                for (int i = 0; i < placements.Count; i++)
                {
                    var placement = placements[i];
                    if (placement.LayoutTransform3DDto is null || placement.Levels.Count == 0)
                        continue;

                    var layout = placement.LayoutTransform3DDto;
                    var first = placement.Levels[0];
                    var bounds = new WarehouseLayoutValidation.LayoutBounds(
                        layout.PositionX,
                        layout.PositionY,
                        layout.PositionZ,
                        layout.RotationY,
                        first.WidthMetres,
                        first.LengthMetres);

                    if (!WarehouseLayoutValidation.FitsWithinContainer(
                            bounds, section.WidthMetres, section.LengthMetres))
                    {
                        context.AddFailure(
                            $"PlacementsRacks[{i}].LayoutTransform3DDto",
                            $"El rack '{placement.Code}' excede las dimensiones de la sección.");
                    }
                }
            });
    }
}
