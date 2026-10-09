using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators;

public class GetPositionDetailValidator : BaseRequestValidator<GetPositionDetailQuery>
{
    public GetPositionDetailValidator()
    {
        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("El Id de la posición es requerido");
    }
}