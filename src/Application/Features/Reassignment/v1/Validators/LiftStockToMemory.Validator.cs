using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class LiftStockToMemoryValidator : BaseRequestValidator<LiftStockToMemoryCommand>
{
    public LiftStockToMemoryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("La sesión de reasignamiento es requerida.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sesión es requerido.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe enviar al menos un polín para levantar.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockId)
                .NotEmpty().WithMessage("El polín es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El id del polín es requerido.");

            item.RuleFor(i => i)
                .Must(i => i.TargetRackPositionId.HasValue ^ i.TargetLotPositionId.HasValue)
                .WithMessage("Cada polín debe tener exactamente una posición destino (rack o tramo).");
        });
    }
}
