using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetWarehouseCapacitiesValidator : BaseRequestValidator<GetWarehouseCapacitiesQuery>
{
    public GetWarehouseCapacitiesValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");
    }
}
