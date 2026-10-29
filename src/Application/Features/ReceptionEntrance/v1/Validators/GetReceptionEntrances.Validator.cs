using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class GetReceptionEntrancesValidator : BaseRequestValidator<GetReceptionEntrancesQuery>
{
    public GetReceptionEntrancesValidator()
    {
        RuleFor(x => x.Date)
            .Must(date => !date.HasValue || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha a filtrar no~ puede ser futura.");
    }
}
