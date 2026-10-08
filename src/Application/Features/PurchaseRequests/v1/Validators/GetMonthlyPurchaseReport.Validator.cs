using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class GetMonthlyPurchaseReportValidator : AbstractValidator<GetMonthlyPurchaseReportQuery>
    {
        public GetMonthlyPurchaseReportValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12)
                .WithMessage("El mes debe estar entre 1 y 12.");

            RuleFor(x => x.Year)
                .InclusiveBetween(2000, 2100)
                .WithMessage("El año debe estar entre 2000 y 2100.");
        }
    }
}
