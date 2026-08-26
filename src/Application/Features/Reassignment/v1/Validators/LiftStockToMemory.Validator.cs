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

        RuleFor(x => x.StockId)
            .NotEmpty().WithMessage("El polín es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del polín es requerido.");
    }
}
