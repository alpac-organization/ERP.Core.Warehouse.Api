using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Validators;

public class GetDucatDetailValidator : BaseRequestValidator<GetDucatDetailQuery>
{
    public GetDucatDetailValidator()
    {
        RuleFor(x => x.DucatId)
            .NotEqual(Guid.Empty)
            .WithMessage("El identificador del DUCA es obligatorio.");
    }
}
