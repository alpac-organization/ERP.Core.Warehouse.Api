using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetWarehousesValidator : BasePagedQueryValidator<GetWarehousesQuery>
{
    public GetWarehousesValidator()
    {
        RuleFor(x => x.WarehouseType)
            .IsInEnum()
            .When(x => x.WarehouseType.HasValue)
            .WithMessage("El tipo de almacén no es válido.");
    }
}