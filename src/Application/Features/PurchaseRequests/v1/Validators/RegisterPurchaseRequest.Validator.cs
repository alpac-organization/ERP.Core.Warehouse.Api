using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class RegisterPurchaseRequestValidator : AbstractValidator<RegisterPurchaseRequestCommand>
    {
        public RegisterPurchaseRequestValidator()
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

            RuleFor(x => x.RequestType)
                .IsInEnum()
                .WithMessage("El tipo de solicitud no es válido.");

            RuleFor(x => x.Observations)
                .MaximumLength(1000)
                .WithMessage("Las observaciones no puede exceder los 1000 caracteres.");

            RuleFor(x => x.PurchaseRequestItems)
                .NotEmpty()
                .WithMessage("Debe agregar al menos un producto a la solicitud.");

            RuleForEach(x => x.PurchaseRequestItems)
                .SetValidator(new RequestedProductValidator());
        }
    }

    public class RequestedProductValidator : AbstractValidator<PurchaseRequestItem>
    {
        public RequestedProductValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("El id del producto es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id del producto no es válido.");

            RuleFor(x => x.UnitMeasureId)
                .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
                .NotEqual(Guid.Empty)
                .WithMessage("La unidad de medida no es válida.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("La cantidad debe ser mayor a cero.");

            RuleFor(x => x.QuantityUnit)
                .GreaterThan(0)
                .When(x => x.QuantityUnit.HasValue)
                .WithMessage("La cantidad por unidad debe ser mayor a cero.");
        }
    }
}