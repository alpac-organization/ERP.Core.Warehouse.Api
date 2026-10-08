using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class RegisterPurchaseRequestCommandValidator : AbstractValidator<RegisterPurchaseRequestCommand>
    {
        public RegisterPurchaseRequestCommandValidator()
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

            RuleFor(x => x.PurchaseRequests)
                .NotEmpty()
                .WithMessage("Debe agregar al menos una solicitud de compra.");

            RuleForEach(x => x.PurchaseRequests)
                .SetValidator(new RegisterPurchaseRequestValidator());
        }
    }

    public class RegisterPurchaseRequestValidator : AbstractValidator<RegisterPurchaseRequest>
    {
        public RegisterPurchaseRequestValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty().WithMessage("El id de la sucursal no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la sucursal es requerido.");

            RuleFor(x => x.CostCenterId)
                .NotEmpty().WithMessage("El centro de costo es requerido")
                .NotEqual(Guid.Empty)
                .WithMessage("El centro de costo es requerido");

            RuleFor(x => x.RequestType)
                .IsInEnum()
                .WithMessage("El tipo de solicitud no es válido.");

            RuleFor(x => x.Destination)
                .IsInEnum()
                .WithMessage("El destino de la solicitud no es válido.");

            RuleFor(x => x.Observations)
                .MaximumLength(1000)
                .WithMessage("Las observaciones no puede exceder los 1000 caracteres.");

            RuleFor(x => x.PriorityLevel)
                .Must(priority => priority.HasValue
                    && priority.Value != PriorityLevel.None
                    && Enum.IsDefined(priority.Value))
                .When(x => x.RequestType == PurchaseRequestType.Requisition)
                .WithMessage("Debe especificar un nivel de prioridad válido cuando el tipo de solicitud es Requisición.");

            RuleFor(x => x.PriorityLevel)
                .Must(priority => !priority.HasValue || priority.Value == PriorityLevel.None)
                .When(x => x.RequestType != PurchaseRequestType.Requisition)
                .WithMessage("El nivel de prioridad solo puede especificarse cuando el tipo de solicitud es Requisición.");

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
                .NotEqual(Guid.Empty)
                .When(x => x.ProductId.HasValue)
                .WithMessage("El id del producto no es válido.");

            RuleFor(x => x.UnitMeasureId)
                .NotEmpty().WithMessage("La unidad de medida es obligatoria.")
                .NotEqual(Guid.Empty)
                .WithMessage("La unidad de medida no es válida.")
                .When(x => x.NewProduct == null);

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("La cantidad debe ser mayor a cero.");

            RuleFor(x => x.QuantityUnit)
                .GreaterThan(0)
                .When(x => x.QuantityUnit.HasValue)
                .WithMessage("La cantidad por unidad debe ser mayor a cero.");

            RuleFor(x => x)
                .Must(x => x.ProductId.HasValue && x.ProductId != Guid.Empty || x.NewProduct is not null)
                .WithMessage("Debe indicar un producto existente o los datos para crear uno nuevo.");

            RuleFor(x => x.NewProduct!)
                .SetValidator(new NewProductPayloadValidator())
                .When(x => x.NewProduct is not null);
        }
    }

    public class NewProductPayloadValidator : AbstractValidator<NewProductPayload>
    {
        public NewProductPayloadValidator()
        {
            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre del producto no puede exceder 200 caracteres.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("La categoría del producto es obligatoria.")
                .NotEqual(Guid.Empty).WithMessage("La categoría del producto no es válida.");

            RuleFor(x => x.UnitMeasureId)
                .NotEmpty().WithMessage("La unidad de medida del producto es obligatoria.")
                .NotEqual(Guid.Empty).WithMessage("La unidad de medida del producto no es válida.");

            RuleFor(x => x.ProductUsageType)
                .IsInEnum()
                .WithMessage("El tipo de uso del producto no es válido.");

            RuleForEach(x => x.Suppliers)
                .ChildRules(supplier =>
                {
                    supplier.RuleFor(s => s.SupplierId)
                        .NotEmpty().WithMessage("El id del proveedor es obligatorio.")
                        .NotEqual(Guid.Empty).WithMessage("El id del proveedor no es válido.");

                    supplier.RuleFor(s => s.UnitPrice)
                        .GreaterThanOrEqualTo(0)
                        .When(s => s.UnitPrice.HasValue)
                        .WithMessage("El precio unitario no puede ser negativo.");
                });
        }
    }

    public class NewProductPayloadValidator : AbstractValidator<NewProductPayload>
    {
        public NewProductPayloadValidator()
        {
            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre del producto no puede exceder 200 caracteres.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("La categoría del producto es obligatoria.")
                .NotEqual(Guid.Empty).WithMessage("La categoría del producto no es válida.");

            RuleFor(x => x.UnitMeasureId)
                .NotEmpty().WithMessage("La unidad de medida del producto es obligatoria.")
                .NotEqual(Guid.Empty).WithMessage("La unidad de medida del producto no es válida.");

            RuleFor(x => x.ProductUsageType)
                .IsInEnum()
                .WithMessage("El tipo de uso del producto no es válido.");

            RuleForEach(x => x.Suppliers)
                .ChildRules(supplier =>
                {
                    supplier.RuleFor(s => s.SupplierId)
                        .NotEmpty().WithMessage("El id del proveedor es obligatorio.")
                        .NotEqual(Guid.Empty).WithMessage("El id del proveedor no es válido.");

                    supplier.RuleFor(s => s.UnitPrice)
                        .GreaterThanOrEqualTo(0)
                        .When(s => s.UnitPrice.HasValue)
                        .WithMessage("El precio unitario no puede ser negativo.");
                });
        }
    }
}
