using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class GetDeletedEvidencesValidator : AbstractValidator<GetDeletedEvidencesQuery>
{
    public GetDeletedEvidencesValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de la compañía es obligatorio.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo es obligatorio.")
            .MaximumLength(50).WithMessage("El código del módulo no puede superar los 50 caracteres.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor que 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor que 0.");
    }
}