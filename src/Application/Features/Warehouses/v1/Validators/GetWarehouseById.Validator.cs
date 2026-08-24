using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetWarehouseByIdValidator : BaseRequestValidator<GetWarehouseByIdQuery>
{
    public GetWarehouseByIdValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id de la bodega no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El id de la bodega no es válido.");
    }
}