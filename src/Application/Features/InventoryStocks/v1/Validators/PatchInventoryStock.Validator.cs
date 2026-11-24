using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Validators
{
    public class PatchInventoryStockValidator : BaseRequestValidator<PatchInventoryStockCommand>
    {
        private const decimal MaxAllowedStockValue = 99999.99m;

        public PatchInventoryStockValidator()
        {
            RuleFor(x => x.InventoryStockId)
                .NotEmpty()
                .WithMessage("El identificador del control de stock es requerido.");

            RuleFor(x => x)
                .Must(x => x.MinimumStock.HasValue || x.MaximumStock.HasValue)
                .WithMessage("Debe proporcionar al menos el stock mínimo o el stock máximo para actualizar.");

            When(x => x.MinimumStock.HasValue, () =>
            {
                RuleFor(x => x.MinimumStock!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("El stock mínimo no puede ser menor a 0.")
                    .LessThanOrEqualTo(MaxAllowedStockValue)
                    .WithMessage($"El stock mínimo no puede exceder {MaxAllowedStockValue:N2}.");
            });

            When(x => x.MaximumStock.HasValue, () =>
            {
                RuleFor(x => x.MaximumStock!.Value)
                    .GreaterThan(0)
                    .WithMessage("El stock máximo debe ser mayor a 0.")
                    .LessThanOrEqualTo(MaxAllowedStockValue)
                    .WithMessage($"El stock máximo no puede exceder {MaxAllowedStockValue:N2}.");
            });

            When(x => x.MinimumStock.HasValue && x.MaximumStock.HasValue, () =>
            {
                RuleFor(x => x.MaximumStock!.Value)
                    .GreaterThan(x => x.MinimumStock!.Value)
                    .WithMessage("El stock máximo debe ser mayor al stock mínimo.");
            });
        }
    }
}