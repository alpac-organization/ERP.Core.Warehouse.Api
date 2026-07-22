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

            RuleForEach(x => x.QuoteDetails)
                .SetValidator(new QuoteDetailsValidator());
        }
    }

    public class QuoteDetailsValidator : AbstractValidator<QuoteDetails>
    {
        public QuoteDetailsValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("La cantidad debe ser mayor a cero.");

            RuleFor(x => x.ProductId)
                .NotEqual(Guid.Empty)
                .WithMessage("El id del producto es requerido.")
                .When(x => !x.IsNewProduct);

            RuleFor(x => x.SupplierId)
                .NotEqual(Guid.Empty)
                .WithMessage("El id del proveedor es requerido.")
                .When(x => !x.IsNewSupplier);

            RuleFor(x => x.UnitMeasureId)
                .NotEqual(Guid.Empty)
                .WithMessage("La unidad de medida es requerida.");

            RuleFor(x => x.AdditionalData)
                .Must(BeValidJson)
                .WithMessage("Los datos adicionales deben ser un JSON válido.");

            RuleFor(x => x.ProductInformation)
                .NotNull()
                .WithMessage("La información del producto es obligatoria para un producto nuevo.")
                .SetValidator(new ProductInformationValidator()!)
                .When(x => x.IsNewProduct);

            RuleFor(x => x.SupplierDatails)
                .NotNull()
                .WithMessage("La información del proveedor es obligatoria para un proveedor nuevo.")
                .SetValidator(new SupplierDatailsValidator()!)
                .When(x => x.IsNewSupplier);
        }

        private bool BeValidJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var _ = System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }
    }

    public class ProductInformationValidator : AbstractValidator<ProductInformation>
    {
        public ProductInformationValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .WithMessage("La categoría del producto es obligatoria.");

            RuleFor(x => x.ProductName)
                .NotEmpty()
                .WithMessage("El nombre del producto es obligatorio.")
                .MaximumLength(150)
                .WithMessage("El nombre del producto no puede exceder los 150 caracteres.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("La descripción no puede exceder los 500 caracteres.");

            RuleFor(x => x.UsageType)
                .IsInEnum()
                .WithMessage("El tipo de uso del producto no es válido.");
        }
    }

    public class SupplierDatailsValidator : AbstractValidator<SupplierDatails>
    {
        public SupplierDatailsValidator()
        {
            RuleFor(x => x.SupplierName)
                .NotEmpty()
                .WithMessage("El nombre del proveedor es obligatorio.")
                .MaximumLength(150)
                .WithMessage("El nombre del proveedor no puede exceder los 150 caracteres.");

            RuleFor(x => x.ContactPhoneNumber)
                .MaximumLength(20)
                .WithMessage("El número de teléfono no puede exceder los 20 caracteres.");

            RuleFor(x => x.IdentificationNumber)
                .NotEmpty()
                .WithMessage("El número de identificación es obligatorio.")
                .MaximumLength(20)
                .WithMessage("El número de identificación no puede exceder los 20 caracteres.");

            RuleFor(x => x.IdentificationType)
                .IsInEnum()
                .WithMessage("El tipo de identificación no es válido.");

            RuleFor(x => x.ConstitutionType)
                .IsInEnum()
                .WithMessage("El tipo de constitución no es válido.");
        }
    }
}