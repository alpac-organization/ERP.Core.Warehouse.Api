using FluentValidation;

using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class CreateAssignmentMachineryValidator : BaseRequestValidator<CreateAssignmentMachineryCommand>
    {
        public CreateAssignmentMachineryValidator()
        {
            RuleFor(x => x.OperationalOrderId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("La orden operativa es requerida");

            RuleFor(x => x.AssignmentOperationalId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("La asignacion operativa es requerida");

            RuleFor(x => x.Machinery)
                .NotEmpty()
                .WithMessage("Debe enviar al menos una maquinaria a asignar");

            RuleForEach(x => x.Machinery)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la maquinaria es requerido");
        }
    }
}
