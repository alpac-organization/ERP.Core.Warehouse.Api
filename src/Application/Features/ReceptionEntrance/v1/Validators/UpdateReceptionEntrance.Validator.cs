using System.Data;
using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

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
            .MaximumLength(50).WithMessage("El país de procedencia no puede exceder 50 caracteres.")
            .When(x => x.CountryOfOrigin is not null);

        RuleFor(x => x.CustomBranchId)
            .NotEqual(Guid.Empty).WithMessage("La Aduana de ingreso no es válida.")
            .When(x => x.CustomBranchId is not null);

        RuleFor(x => x.VehiclePlateNumber)
            .NotEmpty().WithMessage("El número de placa es obligatorio.")
            .MaximumLength(30).WithMessage("El número de placa no puede exceder 30 caracteres.")
            .When(x => x.VehiclePlateNumber is not null);

        RuleFor(x => x.VehicleChassisNumber)
            .NotEmpty().WithMessage("El número de chasis/remolque es obligatorio.")
            .MaximumLength(30).WithMessage("El número de chasis/remolque no puede exceder 30 caracteres.")
            .When(x => x.VehicleChassisNumber is not null);

        RuleFor(x => x.ContainerNumber)
            .NotEmpty().WithMessage("El número de contenedor es obligatorio.")
            .MaximumLength(30).WithMessage("El número de contenedor no puede exceder 30 caracteres.")
            .When(x => x.ContainerNumber is not null);

        RuleFor(x => x.DriverLicense)
            .NotEmpty().WithMessage("La licencia del conductor es obligatoria.")
            .MaximumLength(20).WithMessage("La licencia del conductor no puede exceder 20 caracteres.")
            .When(x => x.DriverLicense is not null);

        RuleFor(x => x.Transportista)
            .NotEmpty().WithMessage("La empresa transportista es requerida.")
            .MaximumLength(100).WithMessage("La empresa transportista no puede exceder 100 caracteres.")
            .When(x => x.Transportista is not null);

        RuleFor(x => x.TransportUnit)
            .IsInEnum().WithMessage("La unidad de transporte no es válida.")
            .When(x => x.TransportUnit is not null);

        RuleFor(x => x.DriverName)
            .NotEmpty().WithMessage("El nombre del conductor es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del conductor no puede exceder 100 caracteres.")
            .When(x => x.DriverName is not null);

        RuleFor(x => x.SealNumber)
            .NotEmpty().WithMessage("El número de marchamo es obligatorio.")
            .MaximumLength(50).WithMessage("El número de marchamo no puede exceder 50 caracteres.")
            .When(x => x.SealNumber is not null);

        RuleFor(x => x.EvidenceToDelete)
            .Must(list => list == null || list.Count > 0)
            .WithMessage("La lista de evidencias a eliminar no puede estar vacía.")
            .When(x => x.EvidenceToDelete is not null);

        RuleForEach(x => x.EvidenceToDelete)
            .NotEmpty().WithMessage("La URL de evidencia a eliminar no puede estar vacía.")
            .When(x => x.EvidenceToDelete is not null);

        RuleFor(x => x.EvidenceToAdd)
            .Must(list => list == null || list.Count <= 10)
            .WithMessage("No se permiten más de 10 imágenes de evidencia nuevas.")
            .When(x => x.EvidenceToAdd is not null);

        RuleForEach(x => x.EvidenceToAdd)
            .NotEmpty().WithMessage("La imagen de evidencia no puede estar vacía.")
            .When(x => x.EvidenceToAdd is not null);

        RuleFor(x => x.CustomsDeclarationNumber)
            .NotEmpty().WithMessage("El número de declaración aduanera es obligatorio.")
            .MaximumLength(30).WithMessage("El número de declaración aduanera no puede exceder 30 caracteres.")
            .When(x => x.CustomsDeclarationNumber is not null);

        RuleFor(x => x.Packages)
            .GreaterThan(0).WithMessage("La cantidad de bultos debe ser mayor a cero (0).")
            .When(x => x.Packages is not null);

        RuleFor(x => x.Customer)
            .NotEmpty().WithMessage("El cliente es obligatorio.")
            .MaximumLength(100).WithMessage("El cliente no puede exceder 100 caracteres.")
            .When(x => x.Customer is not null);

        RuleFor(x => x.Product)
            .NotEmpty().WithMessage("El producto es obligatorio.")
            .MaximumLength(100).WithMessage("El producto no puede exceder 100 caracteres.")
            .When(x => x.Product is not null);

        RuleForEach(x => x.Ducats)
            .ChildRules(ducat =>
            {
                ducat.RuleFor(d => d.Id)
                    .NotNull().WithMessage("El Id del DUCA es requerido para actualizar.")
                    .NotEqual(Guid.Empty).WithMessage("El Id del DUCA no es válido.");

                ducat.RuleFor(d => d.DucatNumber)
                    .NotEmpty().WithMessage("El número de Duca no puede estar vacío")
                    .MaximumLength(100).WithMessage("El número de DUCA no puede exceder 100 caracteres.");
            })
            .When(x => x.Ducats is not null);

        RuleFor(x => x.Ducats)
            .Must(list =>
            {
                if (list == null) return true;
                var idsWithValue = list.Select(d => d.Id).Where(id => id.HasValue).Select(id => id!.Value).ToList();
                return idsWithValue.Count == idsWithValue.Distinct().Count();
            })
            .WithMessage("No se puede referenciar el mismo Id de Duca más de una vez en la misma solicitud.")
            .When(x => x.Ducats is not null);

        RuleFor(x => x)
            .Must(x => !(x.Ducats is not null && HasAnyCustomsField(x)))
            .WithMessage("No se pueden enviar campos de DUCA y de Declaración Aduanera en la misma solicitud, ya que un expediente solo puede ser de un tipo de documento.");

        RuleFor(x => x)
            .Must(x =>
                x.Ducats is not null ||
                x.CountryOfOrigin is not null ||
                x.CustomBranchId is not null ||
                x.VehiclePlateNumber is not null ||
                x.VehicleChassisNumber is not null ||
                x.ContainerNumber is not null ||
                x.DriverLicense is not null ||
                x.Transportista is not null ||
                x.TransportUnit is not null ||
                x.DriverName is not null ||
                x.SealNumber is not null ||
                x.EvidenceToAdd is not null ||
                x.EvidenceToDelete is not null ||
                HasAnyCustomsField(x))
            .WithMessage("Debe incluir al menos un campo para actualizar.");
    }

    private static bool HasAnyCustomsField(UpdateReceptionEntranceCommand x) =>
        x.CustomsDeclarationNumber is not null ||
        x.Packages is not null ||
        x.Customer is not null ||
        x.Product is not null;
}