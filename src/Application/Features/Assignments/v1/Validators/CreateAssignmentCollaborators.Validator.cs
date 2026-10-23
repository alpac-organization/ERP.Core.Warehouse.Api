using FluentValidation;

using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class CreateAssignmentCollaboratorsValidator : BaseRequestValidator<CreateAssignmentCollaboratorsCommand>
    {
        public CreateAssignmentCollaboratorsValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("La orden operativa es requerida");

            RuleFor(x => x.AssignmentOperationalId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("La asignacion operativa es requerida");

            RuleFor(x => x.Collaborators)
                .NotEmpty()
                .WithMessage("Debe enviar al menos un colaborador a asignar");

            RuleForEach(x => x.Collaborators)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("El id del colaborador es requerido");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("El rol de los colaboradores no es válido. Valores permitidos: WarehouseAssistant (auxiliar de bodega) o ForkliftOperator (operador de montacargas)");
        }
    }
}
