using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Validators;

public class GetInventoryOutputsValidator : BaseRequestValidator<GetInventoryOutputsQuery>
{
    public GetInventoryOutputsValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero.");
    }
}