using ERP.Core.Manager.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;
public class CreateReceptionEntranceValidator : AbstractValidator<CreateReceptionEntranceCommand>
{
    public CreateReceptionEntranceValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al ususario autenticado.");

        RuleFor(x => x.DocumentType)
            .Must(dt => dt == DocumentType.DUCA || dt == DocumentType.CustomsDeclaration)
            .WithMessage("El tipo de documento debe ser DUCA o Declaración Aduanera.");

        When(x => x.DocumentType == DocumentType.CustomsDeclaration, () =>
        {
           RuleFor(x => x.CustomsDeclarationNumber)
            .NotEmpty().WithMessage("El número de declaración aduanera es obligatorio.");
           
           RuleFor(x => x.Packages)
            .NotNull().WithMessage("La cantidad de Bultos es obligatoria.")
            .GreaterThan(0).WithMessage("La cantidad de bultos debe ser mayor a cero (0).");
           
           RuleFor(x => x.Customer)
            .NotEmpty().WithMessage("El cliente es obligatorio.");
           
           RuleFor(x => x.Product)
            .NotEmpty().WithMessage("El producto es obligatorio.");
           
           RuleFor(x => x.ContainerNumber)
            .NotEmpty().WithMessage("El número de contenedor es obligatorio.");
        });
            
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
        
        RuleFor(x => x.TransportUnitId)
            .NotEqual(Guid.Empty).WithMessage("La unidad de transporte es obligatoria.");
        
        RuleFor(x => x.DriverName)
            .NotEmpty().WithMessage("El nombre del conductor es obligatorio.");

        RuleFor(x => x.SealNumber)
            .NotEmpty().WithMessage("El número de marchamo es obligatorio.");
    }
}