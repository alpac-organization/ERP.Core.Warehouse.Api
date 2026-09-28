using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class GetReceptionEntranceDetailsValidator : BaseRequestValidator<GetReceptionEntranceDetailsQuery>
{
    public GetReceptionEntranceDetailsValidator()
    {
        
    }
}
