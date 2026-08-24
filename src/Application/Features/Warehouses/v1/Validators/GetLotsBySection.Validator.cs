using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetLotsBySectionValidator : BasePagedQueryValidator<GetLotsBySectionQuery>
{
    public GetLotsBySectionValidator() : base(100)
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.RackStatus)
            .IsInEnum()
            .When(x => x.RackStatus.HasValue)
            .WithMessage("El estado del tramo no es válido.");
    }
}
