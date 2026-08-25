using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetLotByIdValidator : BaseRequestValidator<GetLotByIdQuery>
{
    public GetLotByIdValidator()
    {
        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.LotId)
            .NotEmpty().WithMessage("El id del tramo es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del tramo no es válido.");
    }
}
