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

        RuleFor(x => x.CountryOfOrigin)
            .NotEmpty().WithMessage("El país de procedencia es obligatorio.")
            .When(x => x.CountryOfOrigin is not null);

        RuleFor(x => x.Aduana)
            .NotEmpty().WithMessage("La Aduana de ingreso es obligatoria.")
            .When(x => x.Aduana is not null);

        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("El número de placa es obligatorio.")
            .When(x => x.PlateNumber is not null);

        RuleFor(x => x.TrailerChassis)
            .NotEmpty().WithMessage("El número de chasis/remolque es obligatorio.")
            .When(x => x.TrailerChassis is not null);

        RuleFor(x => x.DriverLicense)
            .NotEmpty().WithMessage("La licencia del conductor es obligatoria.")
            .When(x => x.DriverLicense is not null);

        RuleFor(x => x.Transportista)
            .NotEmpty().WithMessage("La empresa transportista es requerida.")
            .When(x => x.Transportista is not null);

        RuleFor(x => x.Medio)
            .NotEmpty().WithMessage("El medio de transporte es obligatorio.")
            .When(x => x.Medio is not null);

        RuleFor(x => x.DriverName)
            .NotEmpty().WithMessage("El nombre del conductor es obligatorio.")
            .When(x => x.DriverName is not null);

        RuleFor(x => x.Consignee)
            .NotEmpty().WithMessage("El consignatario es obligatorio.")
            .When(x => x.Consignee is not null);

        RuleFor(x => x.SealNumber)
            .NotEmpty().WithMessage("El número de marchamo es obligatorio.")
            .When(x => x.SealNumber is not null);

        RuleForEach(x => x.Ducats)
            .ChildRules(ducat =>
            {
                ducat.RuleFor(d => d.Id)
                    .NotNull().WithMessage("El Id del DUCA es requerido para actualizar.")
                    .NotEqual(Guid.Empty).WithMessage("El Id del DUCA no es válido.");

                ducat.RuleFor(d => d.DucatNumber)
                    .NotEmpty().WithMessage("El número de Duca no puede estar vacío");
            })
            .When(x => x.Ducats is not null);

        RuleFor(x => x.Ducats)
            .Must(list =>
            {
                if (list == null) return true;

                var idsWithValue = list
                    .Select(d => d.Id)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                return idsWithValue.Count == idsWithValue.Distinct().Count();
            })
            .WithMessage("No se puede referenciar el mismo Id de Duca más de una vez en la misma solicitud.")
            .When(x => x.Ducats is not null);

        RuleFor(x => x)
            .Must(x =>
                x.Ducats is not null ||
                x.CountryOfOrigin is not null ||
                x.Aduana is not null ||
                x.PlateNumber is not null ||
                x.TrailerChassis is not null ||
                x.DriverLicense is not null ||
                x.Transportista is not null ||
                x.Medio is not null ||
                x.DriverName is not null ||
                x.Consignee is not null ||
                x.SealNumber is not null)
            .WithMessage("Debe incluir al menos un campo para actualizar.");
    }
}