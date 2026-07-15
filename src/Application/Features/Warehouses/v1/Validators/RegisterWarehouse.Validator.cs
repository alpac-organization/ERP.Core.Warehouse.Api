using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterWarehouseValidator : AbstractValidator<RegisterWarehouseCommand>
    {
        public RegisterWarehouseValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("La sucursal es requerida.");

            RuleFor(x => x.WarehouseName)
                .NotEmpty().WithMessage("El nombre del almacén es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre del almacén no puede superar los 150 caracteres.");

            RuleFor(x => x.WarehouseInformation)
                .NotNull().WithMessage("La información del almacén es obligatoria.");

            When(x => x.WarehouseInformation != null, () =>
            {
                RuleFor(x => x.WarehouseInformation.WarehouseType)
                    .IsInEnum().WithMessage("El tipo de almacén no es válido.");

                RuleFor(x => x.WarehouseInformation.TotalArea)
                    .GreaterThan(0).WithMessage("El área total debe ser mayor a 0.");

                RuleFor(x => x.WarehouseInformation.UnusableArea)
                    .GreaterThanOrEqualTo(0).WithMessage("El área no utilizable no puede ser negativa.")
                    .LessThanOrEqualTo(x => x.WarehouseInformation.TotalArea)
                    .WithMessage("El área no utilizable no puede ser mayor que el área total.");

                RuleFor(x => x.WarehouseInformation.MinHeight)
                    .GreaterThan(0).WithMessage("La altura mínima debe ser mayor a 0.");

                RuleFor(x => x.WarehouseInformation.MaxHeight)
                    .GreaterThan(0).WithMessage("La altura máxima debe ser mayor a 0.")
                    .GreaterThanOrEqualTo(x => x.WarehouseInformation.MinHeight)
                    .WithMessage("La altura máxima no puede ser menor que la altura mínima.");

                RuleFor(x => x.WarehouseInformation.RampasCount)
                    .GreaterThanOrEqualTo(0).WithMessage("La cantidad de rampas no puede ser negativa.");

                RuleFor(x => x.WarehouseInformation.ParkingSpacesCount)
                    .GreaterThanOrEqualTo(0).WithMessage("La cantidad de espacios de parqueo no puede ser negativa.");

                RuleFor(x => x.WarehouseInformation.TotalCubicCapacity)
                    .GreaterThan(0).WithMessage("La capacidad cúbica total debe ser mayor a 0.");
            });
        }
    }
}