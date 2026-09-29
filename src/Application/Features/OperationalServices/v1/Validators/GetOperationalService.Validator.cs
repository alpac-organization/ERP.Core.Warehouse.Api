using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Validators;

public class GetOperationalServiceValidator : BaseRequestValidator<GetOperationalServicesQuery>
{
    public GetOperationalServiceValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero.");
    }
}