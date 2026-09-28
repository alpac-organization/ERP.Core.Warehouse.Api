using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Validators;

public class RegisterMachineryValidator : BaseRequestValidator<MachineryCommand>
{
    public RegisterMachineryValidator()
    {
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca de la maquinaria es obligatoria.")
            .MaximumLength(100).WithMessage("La marca no puede exceder los 100 caracteres.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la maquinaria es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("El año de la maquinaria es obligatorio")
            .MaximumLength(4).WithMessage("El año no puede exceder los 4 dígitos.")
            .Matches("^\\d{4}$").WithMessage("El año debe ser un año válido de 4 dígitos.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("El modelo de la maquinaria es obligatorio.")
            .MaximumLength(100).WithMessage("El modelo no puede exceder los 100 caracteres.");

        RuleFor(x => x.SerialNumber)
            .NotEmpty().WithMessage("El número de serie de la maquinaria es obligatorio.")
            .MaximumLength(100).WithMessage("El número de serie no puede exceder los 100 caracteres.");

        RuleFor(x => x.Color)
            .MaximumLength(100).WithMessage("El color no puede exceder los 100 caracteres.");
    }
}