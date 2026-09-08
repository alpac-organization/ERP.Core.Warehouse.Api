using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

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


            RuleFor(x => x.SectionType)
                .IsInEnum().WithMessage("El tipo de sección no es válido.");

            // RuleFor(x => x.StorageType)
            //     .IsInEnum().WithMessage("El tipo de almacenamiento de la sección no es válido.");

            // Pasillo: no almacena racks ni tramos
            // When(x => x.SectionType == SectionType.Aisle, () =>
            // {
            //     RuleFor(x => x.StorageType)
            //         .Equal(SectionStorageType.Empty)
            //         .WithMessage("Una sección de tipo Pasillo no puede almacenar Racks ni Tramos. StorageType debe ser Empty.");
            // });

            // // Almacenamiento: debe ser Lots o Racks (coincide con el frontend)
            // When(x => x.SectionType != SectionType.Aisle, () =>
            // {
            //     RuleFor(x => x.StorageType)
            //         .Must(st => st is SectionStorageType.Lots or SectionStorageType.Racks)
            //         .WithMessage("Una sección de almacenamiento debe tener StorageType Lots o Racks.");
            // });
        }

    }
}