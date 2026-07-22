using System.Data;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class UpdateReceptionEntranceValidator : AbstractValidator<UpdateReceptionEntranceCommand>
{
    public UpdateReceptionEntranceValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
            .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");
        
        RuleFor(x => x.ReceptionId)
            .NotEmpty().WithMessage("El idetificador de la recepción es obligatorio.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de la recepción no es válido.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.DucatNumbers)
            .NotEmpty().WithMessage("El número de Duca es un dato obligatorio.");

        RuleFor(x => x.CountryOfOrigin)
            .NotEmpty().WithMessage("El país de procedencia es obligatorio.");

        RuleFor(x => x.Aduana)
            .NotEmpty().WithMessage("La Aduana de ingreso es obligatoria.");

        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("El número de placa es obligatorio.");

        RuleFor(x => x.TrailerChassis)
            .NotEmpty().WithMessage("El número de chasis/remolque es obligatorio.");

        RuleFor(x => x.DriverLicense)
            .NotEmpty().WithMessage("La licencia del conductor es obligatoria.");

        RuleFor(x => x.Transportista)
            .NotEmpty().WithMessage("La empresa transportista es requerida.");

        RuleFor(x => x.Medio)
            .NotEmpty().WithMessage("El medio de transporte es obligatorio.");

        RuleFor(x => x.DriverName)
            .NotEmpty().WithMessage("El nombre del conductor es obligatorio.");

        RuleFor(x => x.Consignee)
            .NotEmpty().WithMessage("El consignatario es obligatorio.");

        RuleFor(x => x.SealNumber)
            .NotEmpty().WithMessage("El número de marchamo es obligatorio.");
    }
}