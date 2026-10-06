using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Validators
{
    public class ReceptionInformationOperationalValidator : BaseRequestValidator<ReceptionInformationOperationalCommand>
    {
        public ReceptionInformationOperationalValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .WithMessage("El id de la orden operacional es requerido.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la orden operacional no es válido.");


            RuleFor(x => x.MerchandiseWeight)
                .GreaterThan(0)
                .When(x => x.MerchandiseWeight.HasValue)
                .WithMessage("El peso de la mercancía debe ser mayor que cero.");

            RuleFor(x => x.PackageAmount)
                .GreaterThan(0)
                .When(x => x.PackageAmount.HasValue)
                .WithMessage("La cantidad de bultos debe ser mayor que cero.");


            // Si Merchandises es null → no hay nada que validar (es opcional).
            // Si Merchandises es [] → válido (no-op).
            // Si Merchandises tiene items → cada item debe tener al menos
            //   Merchandise o MerchandiseDescription no vacíos.

            RuleForEach(x => x.Merchandises)
                .ChildRules(merchandise =>
                {
                    merchandise.RuleFor(m => m)
                        .Must(m => !string.IsNullOrWhiteSpace(m.Merchandise)
                                   || !string.IsNullOrWhiteSpace(m.MerchandiseDescription))
                        .WithMessage("Cada mercancía debe tener al menos un nombre o una descripción.");
                })
                .When(x => x.Merchandises is { Count: > 0 });
        }
    }
}