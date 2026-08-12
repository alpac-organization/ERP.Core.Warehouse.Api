using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterWarehouseValidator : AbstractValidator<RegisterWarehouseCommand>
    {
        public RegisterWarehouseValidator(IUnitOfWork unitOfWork)
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.BranchId)
                .NotEmpty().WithMessage("La sucursal es requerida.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código del almacén es obligatorio.")
                .MaximumLength(20).WithMessage("El código del almacén no puede superar los 20 caracteres.")
                .MustAsync(async (code, cancellationToken) =>
                {
                    var exists = await unitOfWork.Warehouses.Entities
                        .AnyAsync(w => w.Code == code, cancellationToken);
                    return !exists;
                })
                .WithMessage("Ya existe un almacén registrado con este código.");

            RuleFor(x => x.WarehouseName)
                .NotEmpty().WithMessage("El nombre del almacén es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre del almacén no puede superar los 150 caracteres.");

            RuleFor(x => x.WarehouseType)
                .IsInEnum().WithMessage("El tipo de almacén no es válido.");

            RuleFor(x => x.ParentWarehouseId)
                .NotEqual(Guid.Empty).When(x => x.ParentWarehouseId.HasValue)
                .WithMessage("El almacén padre no es válido.");

            RuleFor(x => x.WarehouseDetails)
                .NotNull().WithMessage("Los detalles del almacén son obligatorios.");

            When(x => x.WarehouseDetails != null, () =>
            {
                RuleFor(x => x.WarehouseDetails.WidthMetres)
                    .GreaterThan(0).WithMessage("El ancho debe ser mayor a 0.");

                RuleFor(x => x.WarehouseDetails.LengthMetres)
                    .GreaterThan(0).WithMessage("El largo debe ser mayor a 0.");

                RuleFor(x => x.WarehouseDetails.RampsCount)
                    .GreaterThanOrEqualTo(0).When(x => x.WarehouseDetails.RampsCount.HasValue)
                    .WithMessage("La cantidad de rampas no puede ser negativa.");

                RuleFor(x => x.WarehouseDetails.ParkingSpacesCount)
                    .GreaterThanOrEqualTo(0).When(x => x.WarehouseDetails.ParkingSpacesCount.HasValue)
                    .WithMessage("La cantidad de espacios de parqueo no puede ser negativa.");
            });

            // Duplicado: mismo nombre + mismas dimensiones dentro de la misma sucursal
            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var duplicate = await unitOfWork.Warehouses.Entities
                        .Include(w => w.Details)
                        .AnyAsync(w =>
                            w.BranchId == command.BranchId &&
                            w.WarehouseName == command.WarehouseName &&
                            w.Details.WitdhMetres == command.WarehouseDetails.WidthMetres &&
                            w.Details.LengthMetres == command.WarehouseDetails.LengthMetres,
                            cancellationToken);

                    return !duplicate;
                })
                .WithMessage("Ya existe un almacén registrado con el mismo nombre y dimensiones en esta sucursal.")
                .WithName("WarehouseName");
        }
    }
}