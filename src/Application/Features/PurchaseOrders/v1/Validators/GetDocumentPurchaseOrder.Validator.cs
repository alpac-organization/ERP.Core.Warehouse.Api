using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Validators
{
    public class GetDocumentPurchaseOrderValidator : AbstractValidator<GetDocumentPurchaseOrderQuery>
    {
        public GetDocumentPurchaseOrderValidator()
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

            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty().WithMessage("El identificador de la orden de compra no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la orden de compra no es válido.");
        
            RuleFor(x => x.PaymentMethod)
                .IsInEnum()
                .WithMessage("El método de pago no es válido.");
        }
    }
}
