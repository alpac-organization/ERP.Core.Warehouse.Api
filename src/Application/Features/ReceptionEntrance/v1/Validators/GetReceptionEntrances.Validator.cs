using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class GetReceptionEntrancesValidator : BaseRequestValidator<GetReceptionEntrancesQuery>
{
    public GetReceptionEntrancesValidator()
    {
        
    }
}
