using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class UpdateAssignmentValidator : BaseRequestValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("El ID de la asignación es requerido");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("El estado debe ser un valor de enum válido")
                .When(x => x.Status.HasValue);

            When(x => x.Enclosure != null, () =>
            {
                RuleFor(x => x.Enclosure!.Merchandise)
                    .NotEmpty().WithMessage("La mercancía es requerida en el recinto");

                RuleFor(x => x.Enclosure!.MerchandiseDescription)
                    .NotEmpty().WithMessage("La descripción de la mercancía es requerida en el recinto");

                RuleFor(x => x.Enclosure!.DestinationType)
                    .IsInEnum().WithMessage("El tipo de destino debe ser un valor de enum válido");
            });

            When(x => x.Machineries.Count > 0, () =>
            {
                RuleForEach(x => x.Machineries)
                    .ChildRules(m => 
                    {
                        m.RuleFor(m => m.MachineryId)
                            .NotEmpty().WithMessage("El ID de la maquinaria es requerido");
                    });
            });

            When(x => x.Collaborators.Count > 0, () =>
            {
                RuleForEach(x => x.Collaborators)
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