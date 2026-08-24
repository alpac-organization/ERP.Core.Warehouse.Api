using FluentValidation;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Commons.Bases;

public abstract class BaseRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : BaseRequest
{
    protected BaseRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("El id de la empresa no puede estar vacío.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la empresa es requerido");

        RuleFor(x => x.ModuleCode)
            .NotEmpty()
            .WithMessage("El codigo de modulo es requerido.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El id de usuario es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("No se pudo identificar al usuario autenticado.");
    }
}