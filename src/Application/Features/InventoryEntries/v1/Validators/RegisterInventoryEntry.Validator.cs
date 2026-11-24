using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Validators
{
    public class RegisterInventoryEntryValidator : BaseRequestValidator<RegisterInventoryEntryCommand>
    {
        private const int MaxEvidenceImages = 10;
        private const int MaxInvoiceNumberLength = 50;
        private const int MaxObservationsLength = 500;

        public RegisterInventoryEntryValidator()
        {
            RuleFor(x => x.InventoryEntryId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la entrada de inventario es obligatorio.");

            RuleFor(x => x.InvoiceNumber)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("El número de factura es obligatorio.")
                .MaximumLength(MaxInvoiceNumberLength)
                .WithMessage($"El número de factura no puede superar los {MaxInvoiceNumberLength} caracteres.");

            When(x => !string.IsNullOrWhiteSpace(x.Observations), () =>
            {
                RuleFor(x => x.Observations!)
                    .MaximumLength(MaxObservationsLength)
                    .WithMessage($"Las observaciones no pueden superar los {MaxObservationsLength} caracteres.");
            });

            When(x => x.InvoiceDate.HasValue, () =>
            {
                RuleFor(x => x.InvoiceDate!.Value)
                    .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
                    .WithMessage("La fecha de la factura no puede ser una fecha futura.");
            });

            RuleFor(x => x.EvidenceImagesBase64)
                .NotEmpty()
                .WithMessage("Debe adjuntar al menos una imagen de evidencia de la factura.")
                .Must(images => images.Count <= MaxEvidenceImages)
                .WithMessage($"Solo se permiten hasta {MaxEvidenceImages} imágenes de evidencia.");

            RuleForEach(x => x.EvidenceImagesBase64)
                .Must(image => !string.IsNullOrWhiteSpace(image))
                .WithMessage("Una de las imágenes de evidencia está vacía o no es válida.");

            When(x => x.Items is { Count: > 0 }, () =>
            {
                RuleForEach(x => x.Items!).ChildRules(item =>
                {
                    item.RuleFor(x => x.PurchaseOrderItemId)
                        .NotEqual(Guid.Empty)
                        .WithMessage("Cada ítem debe referenciar un ítem válido de la orden de compra.");

                    item.When(x => x.CostCenterId.HasValue, () =>
                    {
                        item.RuleFor(x => x.CostCenterId!.Value)
                            .NotEqual(Guid.Empty)
                            .WithMessage("El centro de costo indicado no es válido.");
                    });
                });

                RuleFor(x => x.Items!)
                    .Must(items => items.Select(item => item.PurchaseOrderItemId).Distinct().Count() == items.Count)
                    .WithMessage("No se puede repetir el mismo ítem de la orden de compra en la entrada.");
            });
        }
    }
}
