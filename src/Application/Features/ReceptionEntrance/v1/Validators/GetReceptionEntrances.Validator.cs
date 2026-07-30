using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class GetReceptionEntrancesValidator : AbstractValidator<GetReceptionEntrancesQuery>
{
    public GetReceptionEntrancesValidator()
    {
        RuleFor(x => x.CompanyId)
                .NotEmpty()
                    .WithMessage("El id de la empresa no puedes vacio.")
                .NotNull()
                    .WithMessage("El id de la empresa es requerido");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                    .WithMessage("El codigo de modulo es requerido")
                .NotNull()
                    .WithMessage("El codigo de modulo es requerido");

            RuleFor(x => x.UserId)
                .NotEmpty()
                    .WithMessage("El id de usuario es requerido")
                .NotNull()
                    .WithMessage("El id de usuario es requerido");
            
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("El número de página debe ser mayor que 0.");
            
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("El tamaño de página debe ser mayor que 0.");
    }
}

public class GetReceptionEntranceDetailValidator : AbstractValidator<GetReceptionEntranceDetailQuery>
{
    public GetReceptionEntranceDetailValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("El Id de la empresa es requerido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo es requerido.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se puede identificar al usuario.");

        RuleFor(x => x.RecordId)
            .NotEqual(Guid.Empty).WithMessage("El identificador de la recepción es obligatorio.");
    }
}

public class GetTransportUnitsValidator : AbstractValidator<GetTreansportUnitsQuery>
{
    public GetTransportUnitsValidator()
    {
        RuleFor(x => x.CompanyId).NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido");
        RuleFor(x => x.ModuleCode).NotEmpty().WithMessage("El código de módulo es requerido.");
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");
    }
}