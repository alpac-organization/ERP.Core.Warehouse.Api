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
        }
    }
}