using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class GetAvailablePositionsValidator : BaseRequestValidator<GetAvailablePositionsQuery>
{
    public GetAvailablePositionsValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén es requerido.");

        RuleFor(x => x.Status)
            .Must(s => s is null || s is "Free" or "Reserved" or "Occupied" or "Blocked")
            .WithMessage("El estado debe ser Free, Reserved, Occupied o Blocked.");
    }
}
