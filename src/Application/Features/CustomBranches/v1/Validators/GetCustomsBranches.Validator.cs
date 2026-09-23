using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Validators
{
    public class GetCustomsBranchesValidator : AbstractValidator<GetCustomBranchesQuery>
    {
        public GetCustomsBranchesValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");
        }
    }
}
