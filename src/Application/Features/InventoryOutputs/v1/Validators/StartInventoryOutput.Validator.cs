using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Validators
{
    public class StartInventoryOutputValidator : BaseRequestValidator<StartInventoryOutputCommand>
    {
        public StartInventoryOutputValidator()
        {
            RuleFor(x => x.InventoryOutputId)
                .NotEmpty().WithMessage("El Id de la salida de inventario es requerido");

            RuleFor(x => x.Items)
                .NotEmpty().When(x => x.IsAllDispached == false)
                .WithMessage("Debe indicar las cantidades a despachar para despacho parcial");

            RuleFor(x => x.Items)
                .Must(items => items == null || items.All(i => i.DispatchedQuantity > 0))
                .WithMessage("Las cantidades a despachar deben ser mayores a cero");

            RuleFor(x => x.DispatchedJustification)
                .NotEmpty().When(x => x.IsAllDispached == false)
                .WithMessage("La justificación es requerida para despacho parcial");
        }
    }
}