using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;
using ERP.Core.Application.Commons.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class SendPurchaseRequestToManagementReviewValidator : BaseRequestValidator<SendPurchaseRequestToManagementReviewCommand>
    {
        public SendPurchaseRequestToManagementReviewValidator()
        {
            RuleFor(x => x.RequisitionAccountingReviewId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la solicitud de compra no es válido.");
        }
    }
}
