using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators;

public class SendToUnloadingValidator : BaseRequestValidator<SendToUnloadingCommand>
{
    public SendToUnloadingValidator()
    {
        RuleFor(x => x.OperationalOrderId)
            .NotEmpty().WithMessage("El Id de la orden operativa es requerido");

        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("El Id de la asignación es requerido");
    }
}