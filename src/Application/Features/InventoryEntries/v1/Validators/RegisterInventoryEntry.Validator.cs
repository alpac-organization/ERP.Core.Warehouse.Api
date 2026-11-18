using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Validators
{
    public class RegisterInventoryEntryValidator : BaseRequestValidator<RegisterInventoryEntryCommand>
    {
        private const int MaxEvidenceImages = 10;

        public RegisterInventoryEntryValidator()
        {
            RuleFor(x => x.InventoryEntryId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la entrada de inventario es obligatorio.");

            RuleFor(x => x.InvoiceNumber)
                .NotEmpty()
                .WithMessage("El número de factura es obligatorio.")
                .MaximumLength(50)
                .WithMessage("El número de factura no puede superar los 50 caracteres.");

            RuleFor(x => x.EvidenceImagesBase64)
                .NotEmpty()
                .WithMessage("Debe adjuntar al menos una imagen de evidencia de la factura.")
                .Must(images => images.Count <= MaxEvidenceImages)
                .WithMessage($"Solo se permiten hasta {MaxEvidenceImages} imágenes de evidencia.");

            RuleForEach(x => x.EvidenceImagesBase64)
                .NotEmpty()
                .WithMessage("Una de las imágenes de evidencia está vacía o no es válida.");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Debe asignar el centro de costo a los ítems de la entrada.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.PurchaseOrderItemId)
                    .NotEqual(Guid.Empty)
                    .WithMessage("Cada ítem debe referenciar un ítem válido de la orden de compra.");

                item.RuleFor(x => x.CostCenterId)
                    .NotEqual(Guid.Empty)
                    .WithMessage("El centro de costo es obligatorio para cada ítem de la entrada.");
            });

            RuleFor(x => x.Items)
                .Must(items => items.Select(item => item.PurchaseOrderItemId).Distinct().Count() == items.Count)
                .WithMessage("No se puede repetir el mismo ítem de la orden de compra en la entrada.");
        }
    }
}
