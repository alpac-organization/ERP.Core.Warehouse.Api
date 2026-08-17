using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators;

public class CreateReceptionEntranceValidator : AbstractValidator<CreateReceptionEntranceCommand>
{
    public CreateReceptionEntranceValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("No se pudo identificar al usuario autenticado.");

        RuleFor(x => x.DocumentType)
            .Must(dt => dt == DocumentType.DUCA || dt == DocumentType.CustomsDeclaration)
            .WithMessage("El tipo de documento debe ser DUCA o Declaración Aduanera.");

        When(x => x.DocumentType == DocumentType.CustomsDeclaration, () =>
        {
            RuleFor(x => x.CustomsDeclarationNumber)
                .NotEmpty().WithMessage("El número de declaración aduanera es obligatorio.")
                .MaximumLength(30).WithMessage("El número de declaración aduanera no puede exceder 30 caracteres.");

            RuleFor(x => x.Packages)
                .NotNull().WithMessage("La cantidad de Bultos es obligatoria.")
                .GreaterThan(0).WithMessage("La cantidad de bultos debe ser mayor a cero (0).");

            RuleFor(x => x.Customer)
                .NotEmpty().WithMessage("El cliente es obligatorio.")
                .MaximumLength(100).WithMessage("El cliente no puede exceder 100 caracteres.");

            RuleFor(x => x.Product)
                .NotEmpty().WithMessage("El producto es obligatorio.")
                .MaximumLength(100).WithMessage("El producto no puede exceder 100 caracteres.");

            RuleFor(x => x.ContainerNumber)
                .NotEmpty().WithMessage("El número de contenedor es obligatorio.")
                .MaximumLength(30).WithMessage("El número de contenedor no puede exceder 30 caracteres.");

            // Validación cruzada: no debe traer datos de DUCA
            RuleFor(x => x.DucatNumbers)
                .Must(list => list == null || list.Count == 0)
                .WithMessage("No se deben enviar números de DUCA cuando el tipo de documento es Declaración Aduanera.");
        });

        When(x => x.DocumentType == DocumentType.DUCA, () =>
        {
            RuleFor(x => x.DucatNumbers)
                .NotEmpty().WithMessage("Debe indicar al menos un número de DUCA.");

            RuleForEach(x => x.DucatNumbers)
                .NotEmpty().WithMessage("El número de DUCA no puede estar vacío.")
                .MaximumLength(100).WithMessage("El número de DUCA no puede exceder 100 caracteres.");

            // Validación cruzada: no debe traer datos de Declaración Aduanera
            RuleFor(x => x.CustomsDeclarationNumber)
                .Must(v => string.IsNullOrWhiteSpace(v))
                .WithMessage("No se debe enviar número de declaración aduanera cuando el tipo de documento es DUCA.");

            RuleFor(x => x.Packages)
                .Must(v => v == null)
                .WithMessage("No se debe enviar cantidad de bultos cuando el tipo de documento es DUCA.");

            RuleFor(x => x.Customer)
                .Must(v => string.IsNullOrWhiteSpace(v))
                .WithMessage("No se debe enviar cliente cuando el tipo de documento es DUCA.");

            RuleFor(x => x.Product)
                .Must(v => string.IsNullOrWhiteSpace(v))
                .WithMessage("No se debe enviar producto cuando el tipo de documento es DUCA.");

            RuleFor(x => x.ContainerNumber)
                .NotEmpty().WithMessage("El número de contenedor es obligatorio.")
                .MaximumLength(30).WithMessage("El número de contenedor no puede exceder 30 caracteres.");
        });

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La hora de inicio es obligatoria.");

        RuleFor(x => x.CountryOfOrigin)
            .NotEmpty().WithMessage("El país de procedencia es obligatorio.")
            .MaximumLength(50).WithMessage("El país de procedencia no puede exceder 50 caracteres.");

        RuleFor(x => x.CustomBranchId)
            .NotEqual(Guid.Empty).WithMessage("La Aduana de ingreso es obligatoria.");

        RuleFor(x => x.VehiclePlateNumber)
            .NotEmpty().WithMessage("El número de placa es obligatorio.")
            .MaximumLength(30).WithMessage("El número de placa no puede exceder 30 caracteres.");

        RuleFor(x => x.VehicleChassisNumber)
            .NotEmpty().WithMessage("El número de chasis/remolque es obligatorio.")
            .MaximumLength(30).WithMessage("El número de chasis/remolque no puede exceder 30 caracteres.");

        RuleFor(x => x.DriverLicense)
            .NotEmpty().WithMessage("La licencia del conductor es obligatoria.")
            .MaximumLength(20).WithMessage("La licencia del conductor no puede exceder 20 caracteres.");

        RuleFor(x => x.Transportista)
            .NotEmpty().WithMessage("La empresa transportista es requerida.")
            .MaximumLength(100).WithMessage("La empresa transportista no puede exceder 100 caracteres.");

        RuleFor(x => x.TransportUnit)
            .IsInEnum().WithMessage("La unidad de transporte es obligatoria.");

        RuleFor(x => x.DriverName)
            .NotEmpty().WithMessage("El nombre del conductor es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del conductor no puede exceder 100 caracteres.");

        RuleFor(x => x.SealNumber)
            .NotEmpty().WithMessage("El número de marchamo es obligatorio.")
            .MaximumLength(50).WithMessage("El número de marchamo no puede exceder 50 caracteres.");
    }
}