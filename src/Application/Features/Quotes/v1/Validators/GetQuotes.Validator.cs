using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class GetQuotesValidator : AbstractValidator<GetQuotesQuery>
{
    public GetQuotesValidator()
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