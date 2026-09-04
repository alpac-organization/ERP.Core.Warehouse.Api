using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterSectionValidator : AbstractValidator<RegisterSectionCommand>
    {
        public RegisterSectionValidator(IUnitOfWork unitOfWork)
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("El almacén es requerido.")
                .MustAsync(async (warehouseId, cancellationToken) =>
                {
                    return await unitOfWork.Warehouses.Entities
                        .AnyAsync(w => w.Id == warehouseId && w.IsActive, cancellationToken);
                })
                .WithMessage("El almacén indicado no existe o no está activo.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código de la sección es obligatorio.")
                .MaximumLength(50).WithMessage("El código de la sección no puede superar los 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de la sección es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre de la sección no puede superar los 150 caracteres.");

            RuleFor(x => x.SectionType)
                .IsInEnum().WithMessage("El tipo de sección no es válido.");

            RuleFor(x => x.StorageType)
                .IsInEnum().WithMessage("El tipo de almacenamiento de la sección no es válido.");

            // Pasillo: no almacena racks ni tramos
            When(x => x.SectionType == SectionType.Aisle, () =>
            {
                RuleFor(x => x.StorageType)
                    .Equal(SectionStorageType.Empty)
                    .WithMessage("Una sección de tipo Pasillo no puede almacenar Racks ni Tramos. StorageType debe ser Empty.");
            });

            // Almacenamiento: debe ser Lots o Racks (coincide con el frontend)
            When(x => x.SectionType != SectionType.Aisle, () =>
            {
                RuleFor(x => x.StorageType)
                    .Must(st => st is SectionStorageType.Lots or SectionStorageType.Racks)
                    .WithMessage("Una sección de almacenamiento debe tener StorageType Lots o Racks.");
            });

            RuleFor(x => x.WidthMetres)
                .GreaterThan(0).WithMessage("El ancho debe ser mayor a 0.");

            RuleFor(x => x.LengthMetres)
                .GreaterThan(0).WithMessage("El largo debe ser mayor a 0.");

            When(x => x.OverflowCapacity is { IsOverflowEnabled: true }, () =>
            {
                RuleFor(x => x.OverflowCapacity!.MaxOverflowPolines)
                    .NotNull().WithMessage("Debe indicar el máximo de polines en desborde si el desborde está habilitado.")
                    .GreaterThan(0).When(x => x.OverflowCapacity!.MaxOverflowPolines.HasValue)
                    .WithMessage("El máximo de polines en desborde debe ser mayor a 0.");
            });

            // Code único dentro del mismo almacén
            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var exists = await unitOfWork.Sections.Entities
                        .AnyAsync(s =>
                            s.WarehouseId == command.WarehouseId &&
                            s.Code == command.Code,
                            cancellationToken);

                    return !exists;
                })
                .WithMessage("Ya existe una sección registrada con este código en este almacén.")
                .WithName("Code");

            // Duplicado: mismo nombre + mismas dimensiones dentro del mismo almacén
            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var duplicate = await unitOfWork.Sections.Entities
                        .AnyAsync(s =>
                            s.WarehouseId == command.WarehouseId &&
                            s.Name == command.Name &&
                            s.WidthMetres == command.WidthMetres &&
                            s.LengthMetres == command.LengthMetres,
                            cancellationToken);

                    return !duplicate;
                })
                .WithMessage("Ya existe una sección registrada con el mismo nombre y dimensiones en este almacén.")
                .WithName("Name");

            When(x => x.LayoutTransform3DDto != null, () =>
            {
                RuleFor(x => x.LayoutTransform3DDto!)
                    .Must(WarehouseLayoutValidation.HasValidNonNegativeCoordinates)
                    .WithMessage("Las coordenadas X, Y, & Z no pueden ser negativas, Ingrese las correctas");

                RuleFor(x => x.LayoutTransform3DDto!.RotationY)
                .Must(WarehouseLayoutValidation.IsRightAngleRotation)
                .WithMessage("La rotación debe ser un ángulo recto (0, 90, 180, 270).");


                RuleFor(x => x)
                    .MustAsync(async (command, cancellationToken) =>
                    {
                        var warehouse = await unitOfWork.Warehouses.Entities
                            .AsNoTracking()
                            .Where(w => w.Id == command.WarehouseId)
                            .Select(w => new
                            {
                                Width = w.Details.WitdhMetres,
                                Length = w.Details.LengthMetres
                            })
                            .FirstOrDefaultAsync(cancellationToken);

                        if (warehouse is null) return false;

                        var layout = command.LayoutTransform3DDto!;
                        var bounds = new WarehouseLayoutValidation.LayoutBounds(
                            layout.PositionX,
                            layout.PositionY,
                            layout.PositionZ,
                            layout.RotationY,
                            command.WidthMetres,
                            command.LengthMetres
                        );
                        return WarehouseLayoutValidation.FitsWithinContainer(
                            bounds,
                            warehouse.Width,
                            warehouse.Length
                         );
                    })
                    .WithMessage("La sección excede los límites del almacén.");

            });
        }

    }
}