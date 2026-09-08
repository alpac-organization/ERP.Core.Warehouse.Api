using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

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
                .MaximumLength(20).WithMessage("El código del almacén no puede superar los 20 caracteres.");

            RuleFor(x => x.WarehouseName)
                .NotEmpty().WithMessage("El nombre del almacén es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre del almacén no puede superar los 150 caracteres.");

            RuleFor(x => x.WarehouseType)
                .IsInEnum().WithMessage("El tipo de almacén no es válido.");

            RuleFor(x => x.ParentWarehouseId)
                .NotEqual(Guid.Empty).When(x => x.ParentWarehouseId.HasValue)
                .WithMessage("El almacén padre no es válido.");


            // your validation
        }
    }
}