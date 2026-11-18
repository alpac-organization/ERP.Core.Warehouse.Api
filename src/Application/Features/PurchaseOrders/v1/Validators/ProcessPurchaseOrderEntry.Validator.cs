using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Validators
{
    public class ProcessPurchaseOrderEntryValidator : BaseRequestValidator<ProcessPurchaseOrderEntryCommand>
    {
        public ProcessPurchaseOrderEntryValidator()
        {
            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty()
                .WithMessage("El identificador de la orden de compra es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la orden de compra no es válido.");
        }
    }
}
