using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Validators;

public class CreateSupplyValidator : BaseRequestValidator<RegisterSupplyCommand>
{
    public CreateSupplyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del insumo es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre del insumo no puede superar los 200 caracteres.")
            .Must(name => !string.IsNullOrWhiteSpace(name.SanitizeAlphanumeric()))
            .WithMessage("El nombre del insumo debe contener caracteres válidos.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción del insumo no puede superar los 500 caracteres.");
    }
}
