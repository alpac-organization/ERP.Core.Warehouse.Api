using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class UpdatePurchaseCommandValidator : AbstractValidator<UpdatePurchaseCommand>
    {
        public UpdatePurchaseCommandValidator()
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

            RuleFor(x => x.PurchaseRequestId)
                .NotEmpty().WithMessage("El id de la solicitud de compra es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la solicitud de compra no es válido.");

            RuleFor(x => x.Observations)
                .MaximumLength(1000)
                .When(x => x.Observations != null)
                .WithMessage("Las observaciones no puede exceder los 1000 caracteres.");

            RuleFor(x => x.PriorityLevel)
                .IsInEnum()
                .When(x => x.PriorityLevel.HasValue)
                .WithMessage("El nivel de prioridad no es válido.");

            RuleFor(x => x.DestinationRequest)
                .IsInEnum()
                .When(x => x.DestinationRequest.HasValue)
                .WithMessage("El destino de la solicitud no es válido.");

            RuleForEach(x => x.PurchaseRequestItems)
                .SetValidator(new UpdatePurchaseRequestItemValidator())
                .When(x => x.PurchaseRequestItems != null);
        }
    }

    public class UpdatePurchaseRequestItemValidator : AbstractValidator<UpdatePurchaseRequestItem>
    {
        public UpdatePurchaseRequestItemValidator()
        {
            RuleFor(x => x.Id)
                .NotEqual(Guid.Empty)
                .When(x => x.Id.HasValue)
                .WithMessage("El id del ítem no es válido.");

            RuleFor(x => x.ProductId)
                .NotEqual(Guid.Empty)
                .When(x => x.ProductId.HasValue)
                .WithMessage("El id del producto no es válido.");

            RuleFor(x => x.UnitMeasureId)
                .NotEqual(Guid.Empty)
                .When(x => x.UnitMeasureId.HasValue)
                .WithMessage("La unidad de medida no es válida.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .When(x => x.Quantity.HasValue)
                .WithMessage("La cantidad debe ser mayor a cero.");

            RuleFor(x => x.QuantityUnit)
                .GreaterThan(0)
                .When(x => x.QuantityUnit.HasValue)
                .WithMessage("La cantidad por unidad debe ser mayor a cero.");
        }
    }
}