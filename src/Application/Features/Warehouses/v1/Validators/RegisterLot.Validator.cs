using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterLotsCommandValidator : AbstractValidator<RegisterLotsCommand>
{
   public RegisterLotsCommandValidator(IUnitOfWork _unitOfWork)
   {
      RuleFor(x => x.SectionId).NotEmpty();
      RuleFor(x => x.PlacementsLots).NotEmpty().WithMessage("Debe especificar al menos un grupo de tramos.");

      RuleForEach(x => x.PlacementsLots).ChildRules(placement =>
      {
         placement.RuleFor(p => p.Code)
                    .NotEmpty()
                    .WithMessage("El codigo del tramo es obligatorio");

         placement.RuleFor(p => p.WidthMetres).GreaterThan(0);
         placement.RuleFor(p => p.LengthMetres).GreaterThan(0);
         placement.RuleFor(p => p.NominalRows).GreaterThan(0);
         placement.RuleFor(p => p.NominalColumns).GreaterThan(0);
         placement.RuleFor(p => p.Status).IsInEnum();

         placement.RuleFor(p => p.UnavailableReason)
                    .NotEmpty()
                    .MaximumLength(250)
                    .When(p => p.Status is RackStatus.UnderMaintenance or RackStatus.Blocked);

         placement.RuleFor(p => p.UnavailableReason)
               .Empty()
               .When(p => p.Status is RackStatus.Available or RackStatus.Occupied);

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
      RuleFor(x => x.PlacementsLots)
      .CustomAsync(async (placements, context, cancellationToken) =>
      {
         var command = (RegisterLotsCommand)context.InstanceToValidate;

         var section = await _unitOfWork.Sections.Entities
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
            if (placement.LayoutTransform3DDto == null) continue;

            var layout = placement.LayoutTransform3DDto;

            var bounds = new WarehouseLayoutValidation.LayoutBounds(
                  PositionX: layout.PositionX,
                  PositionY: layout.PositionY,
                  PositionZ: layout.PositionZ,
                  RotationY: layout.RotationY,
                  WidthMetres: placement.WidthMetres,
                  LengthMetres: placement.LengthMetres
              );

            bool isSpatiallyValid = WarehouseLayoutValidation.FitsWithinContainer(
                  bounds,
                  containerWidthMetres: section.WidthMetres,
                  containerLengthMetres: section.LengthMetres
              );

            if (!isSpatiallyValid)
            {
               context.AddFailure($"PlacementsLots[{i}].LayoutTransform3DDto",
                $"El tramo '{placement.Code}' excede las dimensiones de la sección."); return;
            }
         }
      });
   }
}