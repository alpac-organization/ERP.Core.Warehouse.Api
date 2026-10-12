using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Validators
{
    public class UpdateQuotationValidator : AbstractValidator<UpdateQuotationCommand>
    {
        public UpdateQuotationValidator()
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

            RuleFor(x => x.QuotationId)
                .NotEmpty().WithMessage("El id de la cotización no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la cotización no es válido.");

            RuleFor(x => x.SupplierId)
                .NotEqual(Guid.Empty)
                .When(x => x.SupplierId.HasValue)
                .WithMessage("El id del proveedor no es válido.");

            RuleFor(x => x.PaymentMethodType)
                .IsInEnum()
                .When(x => x.PaymentMethodType.HasValue)
                .WithMessage("El método de pago no es válido.");

            RuleFor(x => x.PriceUnit)
                .GreaterThan(0)
                .When(x => x.PriceUnit.HasValue)
                .WithMessage("El precio unitario debe ser mayor a cero.");

            RuleFor(x => x.PreferentialPrice)
                .GreaterThan(0)
                .When(x => x.PreferentialPrice.HasValue)
                .WithMessage("El precio preferencial debe ser mayor a cero.");

            RuleFor(x => x.Currency)
                .IsInEnum()
                .When(x => x.Currency.HasValue)
                .WithMessage("La moneda no es válida.");

            RuleFor(x => x.BrandProduct)
                .MaximumLength(200)
                .When(x => x.BrandProduct is not null)
                .WithMessage("La marca del producto no puede exceder los 200 caracteres.");
        }
    }
}
