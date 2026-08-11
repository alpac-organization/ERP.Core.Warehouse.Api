using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Validators
{
    public class GetQuotationsValidator : AbstractValidator<GetQuotationsQuery>
    {
        public GetQuotationsValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.PurchaseRequestItemId)
                .NotEmpty().WithMessage("El id del ítem de la solicitud no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id del ítem de la solicitud no es válido.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("El número de página debe ser mayor que 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("El tamaño de página debe ser mayor que 0.");
        }
    }
}
