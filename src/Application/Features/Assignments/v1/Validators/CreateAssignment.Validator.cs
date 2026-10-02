using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class CreateAssignmentValidator : BaseRequestValidator<CreateAssignmentCommand>
    {
        public CreateAssignmentValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty().WithMessage("El ID de la orden operativa es requerido");

            RuleFor(x => x.DestinationType)
                .NotEmpty()
                .WithMessage("El lugar de destino de la mercaderia es obligatorio");
                
            RuleFor(x => x.Observations)
                .NotEmpty()
                .WithMessage("Indique un comentario para el bodegero");

            RuleFor(x => x.WarehouseId)
                .NotEmpty()
                .WithMessage("La bodega destino es obligatoria, designe a la bodega que va esta mercaderia")
                .When(x => x.DestinationType == DestinationType.Warehouse);

            When(x => x.AssignedMachineries.Count > 0, () =>
            {
                RuleForEach(x => x.AssignedMachineries)
                    .ChildRules(m => 
                    {
                        m.RuleFor(m => m.MachineryId)
                            .NotEmpty().WithMessage("El ID de la maquinaria es requerido");
                    });
            });

            When(x => x.AssignedCollaborators.Count > 0, () =>
            {
                RuleForEach(x => x.AssignedCollaborators)
                    .ChildRules(c => 
                    {
                        c.RuleFor(c => c.CollaboratorId)
                            .NotEmpty().WithMessage("El ID del colaborador es requerido");
                        
                        c.RuleFor(c => c.Role)
                            .IsInEnum().WithMessage("El rol debe ser un valor de enum válido");
                    });
            });
        }
    }
}