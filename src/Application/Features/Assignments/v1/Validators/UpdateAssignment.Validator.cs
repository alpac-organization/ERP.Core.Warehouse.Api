using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class UpdateAssignmentValidator : BaseRequestValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty()
                .WithMessage("El ID de la asignación es requerido");

            RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .WithMessage("El ID de la orden operativa es requerido");

            When(x => x.DestinationType.HasValue, () =>
            {
                RuleFor(x => x.DestinationType)
                    .IsInEnum()
                    .WithMessage("El tipo de destino no es válido");

                When(x => x.DestinationType == DestinationType.Warehouse, () =>
                {
                    RuleFor(x => x.WarehouseId)
                        .NotEmpty().WithMessage("El ID del almacén es requerido cuando se especifica un tipo de destino");
                });
            });
        }
    }
}