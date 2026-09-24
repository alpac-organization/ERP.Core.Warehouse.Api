using System;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Validators
{
    public static class PurchaseOrderValidationExtensions
    {
        public static IRuleBuilderOptions<T, Guid?> ValidatePurchaseOrderId<T>(this IRuleBuilder<T, Guid?> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("El identificador de la orden de compra no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de la orden de compra no es válido.");
        }

        public static IRuleBuilderOptions<T, Guid> ValidatePurchaseOrderId<T>(this IRuleBuilder<T, Guid> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("El identificador de la orden de compra no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de la orden de compra no es válido.");
        }
    }
}
