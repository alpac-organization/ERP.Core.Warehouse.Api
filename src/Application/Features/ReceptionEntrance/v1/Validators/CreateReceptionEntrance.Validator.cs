using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators
{
    public class CreateReceptionEntranceValidator : AbstractValidator<CreateReceptionEntranceCommand>
    {
        public CreateReceptionEntranceValidator()
        {
        
        }
    }
}
