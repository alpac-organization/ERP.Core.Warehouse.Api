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
                .MaximumLength(50).WithMessage("El código de la sección no puede superar los 50 caracteres.")
                .MustAsync(async (command, code, cancellationToken) =>
                {
                    return !await unitOfWork.Sections.Entities
                        .AnyAsync(s =>
                            s.WarehouseId == command.WarehouseId &&
                            s.Code == code,
                            cancellationToken);
                })
                .WithMessage("Ya existe una sección con ese código en el almacén.");

            RuleFor(x => x.SectionType)
                .IsInEnum().WithMessage("El tipo de sección no es válido.");

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("El ancho debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El ancho admite máximo 2 decimales");

            RuleFor(x => x.Length)
                .GreaterThan(0).WithMessage("El largo debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("El largo admite máximo 2 decimales");
        }
    }
}