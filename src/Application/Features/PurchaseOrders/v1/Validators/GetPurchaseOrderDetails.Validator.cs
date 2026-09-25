using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Validators
{
    public class GetPurchaseOrderDetailsValidator : BaseRequestValidator<GetPurchaseOrderDetailsQuery>
    {
        public GetPurchaseOrderDetailsValidator()
        {
            RuleFor(x => x.PurchaseOrderId).ValidatePurchaseOrderId();
        }
    }
}
