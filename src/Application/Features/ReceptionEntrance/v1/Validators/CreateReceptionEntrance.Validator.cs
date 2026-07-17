using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;
public class CreateReceptionEntranceValidator : AbstractValidator<CreateReceptionEntranceCommand>
{
    public CreateReceptionEntranceValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al ususario autenticado.");
            
        RuleFor(x => x.DucatNumbers)
            .NotEmpty().WithMessage("El número de Duca es un dato obligatorio.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");
     
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La hora de inicio es obligatoria.");
        
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