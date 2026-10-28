using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Validators;

public class GetSupplyRequestsDetailValidator : BaseRequestValidator<GetSupplyRequestsDetailQuery>
{
    public GetSupplyRequestsDetailValidator()
    {
        RuleFor(x => x.SupplyRequestId)
            .NotNull().WithMessage("El id de la solicitud es requerida.");
    }
}