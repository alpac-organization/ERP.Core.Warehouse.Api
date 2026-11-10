using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Validators;

public class GetInventoryOutputsDetailValidator : BaseRequestValidator<GetInventoryOutputsDetailQuery>
{
    public GetInventoryOutputsDetailValidator()
    {
        RuleFor(x => x.InventoryOutputId)
            .NotEqual(Guid.Empty)
            .WithMessage("El identificador de la salida de inventario es requerido.");
    }
}