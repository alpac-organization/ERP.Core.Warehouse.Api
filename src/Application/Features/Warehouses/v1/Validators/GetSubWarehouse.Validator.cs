using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetSubWarehousesValidator : BasePagedQueryValidator<GetSubWarehousesQuery>
{
    public GetSubWarehousesValidator()
    {
        RuleFor(x => x.ParentWarehouseId)
            .NotEmpty()
            .WithMessage("El id de la bodega padre es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la bodega padre no es válido.");
    }
}