using System;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public static class WarehouseSectionValidationExtensions
    {
        public static IRuleBuilderOptions<T, Guid> ValidateWarehouseId<T>(this IRuleBuilder<T, Guid> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("El almacén es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El almacén es requerido.");
        }

        public static IRuleBuilderOptions<T, decimal> ValidateDimension<T>(this IRuleBuilder<T, decimal> ruleBuilder, string dimensionName)
        {
            return ruleBuilder
                .GreaterThan(0).WithMessage($"El {dimensionName} debe ser mayor a cero")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage($"El {dimensionName} admite máximo 2 decimales");
        }
    }
}
