using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Validators;

public class ReserveUnloadingPositionsValidator : BaseRequestValidator<ReserveUnloadingPositionsCommand>
{
    public ReserveUnloadingPositionsValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("El identificador de la asignación no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de la asignación no es válido.");

        RuleFor(x => x.Positions)
            .NotEmpty().WithMessage("Debe seleccionar al menos una posición a reservar.");

        RuleForEach(x => x.Positions).SetValidator(new ReservationItemValidator());
    }
}

public class ReservationItemValidator : AbstractValidator<PositionReservationItemDto>
{
    public ReservationItemValidator()
    {
        RuleFor(x => x.RackPositionId)
            .NotNull().WithMessage("Debe indicar una posición de rack o de lot.")
            .When(x => x.LotPositionId is null);

        RuleFor(x => x.LotPositionId)
            .NotNull().WithMessage("Debe indicar una posición de rack o de lot.")
            .When(x => x.RackPositionId is null);
    }
}