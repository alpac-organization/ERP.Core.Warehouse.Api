using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterLotCommandValidator : AbstractValidator<RegisterLotCommand>
{
    public RegisterLotCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.SectionId).NotEmpty().WithMessage("La sección es obligatoria.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código del tramo es obligatorio.")
            .MaximumLength(50);

        RuleFor(x => x.WidthMetres).GreaterThan(0).WithMessage("El ancho debe ser mayor a 0.");
        RuleFor(x => x.LengthMetres).GreaterThan(0).WithMessage("El largo debe ser mayor a 0.");
        RuleFor(x => x.NominalRows).GreaterThan(0).WithMessage("Las filas deben ser mayor a 0.");
        RuleFor(x => x.NominalColumns).GreaterThan(0).WithMessage("Las columnas deben ser mayor a 0.");
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.UnavailableReason)
            .NotEmpty()
            .MaximumLength(250)
            .When(x => x.Status is RackStatus.UnderMaintenance or RackStatus.Blocked);

        RuleFor(x => x.UnavailableReason)
            .Empty()
            .When(x => x.Status is RackStatus.Available or RackStatus.Occupied);

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

                    if (section is null) return false;

                    var layout = command.LayoutTransform3DDto!;
                    var bounds = new WarehouseLayoutValidation.LayoutBounds(
                        layout.PositionX,
                        layout.PositionY,
                        layout.PositionZ,
                        layout.RotationY,
                        command.WidthMetres,
                        command.LengthMetres);

                    return WarehouseLayoutValidation.FitsWithinContainer(
                        bounds,
                        section.WidthMetres,
                        section.LengthMetres);
                })
                .WithMessage("El tramo excede las dimensiones de la sección.");
        });
    }
}
