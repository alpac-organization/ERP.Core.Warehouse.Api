using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Validators
{
    public class RegisterQuotationValidator : AbstractValidator<RegisterQuotationCommand>
    {
        public RegisterQuotationValidator()
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

            RuleFor(x => x.SupplierId)
                .NotEmpty().WithMessage("El id del proveedor no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id del proveedor no es válido.");
                
            RuleFor(x => x.PurchaseRequestId)
                .NotEmpty().WithMessage("El id de la solicitud de compra no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la solicitud de compra no es válido.");

            RuleFor(x => x.Iva)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Iva.HasValue)
                .WithMessage("El IVA no puede ser negativo.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("El precio debe ser mayor a cero.");

            RuleFor(x => x.PriceUnit)
                .GreaterThan(0)
                .When(x => x.PriceUnit.HasValue)
                .WithMessage("El precio unitario debe ser mayor a cero.");

            RuleFor(x => x.BrandProduct)
                .MaximumLength(200)
                .WithMessage("La marca del producto no puede exceder los 200 caracteres.");

            RuleFor(x => x.DeliveryTime)
                .GreaterThan(0)
                .When(x => x.DeliveryTime.HasValue)
                .WithMessage("El tiempo de entrega debe ser mayor a cero.");

            RuleFor(x => x.WarrantyPeriod)
                .GreaterThan(0)
                .When(x => x.WarrantyPeriod.HasValue)
                .WithMessage("El período de garantía debe ser mayor a cero.");

            RuleFor(x => x.WarrantyPeriodTimeType)
                .IsInEnum()
                .When(x => x.WarrantyPeriodTimeType.HasValue && x.HasGuarantee)
                .WithMessage("El tipo de período de garantía no es válido.");

            RuleFor(x => x.DeliveryTime)
                .NotNull()
                .When(x => x.HasDelivery)
                .WithMessage("Debe indicar el tiempo de entrega si la cotización incluye entrega.");

            RuleFor(x => x.DeliveryTimeType)
                .IsInEnum()
                .When(x => x.DeliveryTimeType.HasValue && x.HasDelivery)
                .WithMessage("El tipo de tiempo de entrega no es válido.");

            RuleFor(x => x.DeliveryTimeType)
                .NotNull()
                .When(x => x.HasDelivery)
                .WithMessage("Debe indicar el tipo de tiempo de entrega si la cotización incluye entrega.");

            RuleFor(x => x.WarrantyPeriod)
                .NotNull()
                .When(x => x.HasGuarantee)
                .WithMessage("Debe indicar el período de garantía si la cotización incluye garantía.");

            RuleFor(x => x.WarrantyPeriodTimeType)
                .NotNull()
                .When(x => x.HasGuarantee)
                .WithMessage("Debe indicar el tipo de período de garantía si la cotización incluye garantía.");
        }
    }
}