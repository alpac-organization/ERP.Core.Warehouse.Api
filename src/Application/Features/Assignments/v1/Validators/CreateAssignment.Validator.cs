using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class CreateAssignmentValidator : BaseRequestValidator<CreateAssignmentCommand>
    {
        public CreateAssignmentValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty().WithMessage("El ID de la orden operativa es requerido");

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