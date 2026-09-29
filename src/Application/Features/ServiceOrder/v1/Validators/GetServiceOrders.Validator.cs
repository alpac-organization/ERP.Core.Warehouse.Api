using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Validators
{
    public class GetServicesOrdersValidator : BaseRequestValidator<GetServiceOrdersQuery>
    {
        public GetServicesOrdersValidator()
        {
        }
    }
}