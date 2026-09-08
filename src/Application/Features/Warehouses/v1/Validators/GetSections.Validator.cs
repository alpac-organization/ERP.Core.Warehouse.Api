using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class GetSectionsValidator : BasePagedQueryValidator<GetSectionsQuery>
{
    public GetSectionsValidator()
    {
        RuleFor(x => x.SectionType)
            .IsInEnum()
            .When(x => x.SectionType.HasValue)
            .WithMessage("El tipo de seccion no es válido.");
    }
}