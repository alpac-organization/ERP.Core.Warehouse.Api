using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Validators
{
    public class RegisterQuoteValidator : AbstractValidator<RegisterQuoteCommand>
    {
        public RegisterQuoteValidator()
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

            RuleFor(x => x.BranchId)
                .NotEmpty().WithMessage("El id de la sucursal no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la sucursal es requerido.");

            RuleFor(x => x.QuoteDate)
                .NotEmpty().WithMessage("La fecha de cotización es obligatoria.")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("La fecha de cotización no puede ser una fecha futura.");

            RuleFor(x => x.Observations)
                .MaximumLength(500)
                .WithMessage("Las observaciones no pueden exceder los 500 caracteres.");

            RuleFor(x => x.QuoteDetails)
                .NotEmpty()
                .WithMessage("Debe incluir al menos un detalle de cotización.");
        }
    }
}