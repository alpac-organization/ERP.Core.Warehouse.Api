using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetRackDetailsValidator : BaseRequestValidator<GetRackDetailsQuery>
{
    public GetRackDetailsValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El almacén es obligatorio.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("La sección es obligatoria.");

        RuleFor(x => x.RackId)
            .NotEmpty().WithMessage("El rack es obligatorio.");
    }
}
