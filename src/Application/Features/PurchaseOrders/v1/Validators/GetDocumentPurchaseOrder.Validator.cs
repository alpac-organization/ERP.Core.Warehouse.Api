using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Validators
{
    public class GetDocumentPurchaseOrderValidator : BaseRequestValidator<GetDocumentPurchaseOrderQuery>
    {
        public GetDocumentPurchaseOrderValidator()
        {
            RuleFor(x => x.PurchaseOrderId).ValidatePurchaseOrderId();

            RuleFor(x => x.PaymentMethod)
                .IsInEnum()
                .WithMessage("El método de pago no es válido.");
        }
    }
}
