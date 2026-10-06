using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetPositionsValidator : BaseRequestValidator<GetPositionsQuery>
{
    public GetPositionsValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.TramoId)
            .NotEqual(Guid.Empty).When(x => x.TramoId.HasValue)
            .WithMessage("El id del tramo no es válido.");

        RuleFor(x => x.RackId)
            .NotEqual(Guid.Empty).When(x => x.RackId.HasValue)
            .WithMessage("El id del rack no es válido.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("El estado de la posición no es válido.");
    }
}
