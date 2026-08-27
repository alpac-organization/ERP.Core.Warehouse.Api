using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Validators;

public class OpenReassignmentSessionValidator : BaseRequestValidator<OpenReassignmentSessionCommand>
{
    public OpenReassignmentSessionValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El almacén es requerido.")
            .NotEqual(Guid.Empty).WithMessage("El id del almacén es requerido.");
    }
}
