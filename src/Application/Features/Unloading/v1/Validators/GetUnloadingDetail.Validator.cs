using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Validators;

public class GetUnloadingDetailValidator : BaseRequestValidator<GetUnloadingDetailQuery>
{
    public GetUnloadingDetailValidator()
    {
        RuleFor(x => x.UnloadingId)
            .NotEmpty().WithMessage("El identificador de la descarga no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de la descarga no es válido.");
    }
}
