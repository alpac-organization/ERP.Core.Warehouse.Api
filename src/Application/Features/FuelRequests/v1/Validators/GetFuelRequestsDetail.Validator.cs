using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Validators;

public class GetFuelRequestsDetailValidator : BaseRequestValidator<GetFuelRequestsDetailQuery>
{
    public GetFuelRequestsDetailValidator()
    {
        RuleFor(x => x.FuelRequestId)
            .NotNull().WithMessage("El id de la solicitud de combustible es requerida.");
    }
}