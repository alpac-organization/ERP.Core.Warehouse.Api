using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class ResolveMemoryItemsValidator : BaseRequestValidator<ResolveMemoryItemsCommand>
{
    public ResolveMemoryItemsValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("La sesión de reasignamiento es requerida.")
            .NotEqual(Guid.Empty).WithMessage("El id de la sesión es requerido.");

        RuleFor(x => x.MemoryItemIds)
            .NotEmpty().WithMessage("Debe enviar al menos un polín en aire para confirmar.");

        RuleForEach(x => x.MemoryItemIds).ChildRules(item =>
        {
            item.RuleFor(i => i)
                .NotEmpty().WithMessage("El id del polín en aire es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El id del polín en aire es requerido.");
        });
    }
}
