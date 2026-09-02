// Validators/RegisterRackCommandValidator.cs
using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterRacksBulkCommandValidator : AbstractValidator<RacksBulkCommand>
{
    public RegisterRacksBulkCommandValidator(IUnitOfWork _unitOfWork)
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.PlacementRacks)
        .NotEmpty()
        .WithMessage("Debe especificar Al menos un rack");


        RuleForEach(x => x.PlacementRacks).ChildRules(placement =>
        {
            placement.RuleFor(p => p.Code).NotEmpty().WithMessage("El código del rack es obligatorio.");
            placement.RuleFor(p => p.WidthMetres).GreaterThan(0);
            placement.RuleFor(p => p.LengthMetres).GreaterThan(0);
            placement.RuleFor(p => p.MaxPulleys).GreaterThan(0);
            placement.RuleFor(p => p.Status).IsInEnum();

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

        RuleFor(x => x.PlacementRacks)
              .CustomAsync(async (placements, context, cancellationToken) =>
              {
                  var command = context.InstanceToValidate;

                  var section = await _unitOfWork.Sections.Entities
                      .AsNoTracking()
                      .FirstOrDefaultAsync(cancellationToken);

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
                        layout.PositionX,
                        layout.PositionY,
                        layout.PositionZ,
                        layout.RotationY,
                        placement.WidthMetres,
                        placement.LengthMetres
                      );

                      if (!WarehouseLayoutValidation.FitsWithinContainer(bounds, section.WidthMetres, section.LengthMetres))
                      {
                          context.AddFailure($"PlacementsRacks[{i}].LayoutTransform3DDto",
                                             $"El rack '{placement.Code}' excede las dimensiones de la sección.");
                      }
                  }
              });
    }
}