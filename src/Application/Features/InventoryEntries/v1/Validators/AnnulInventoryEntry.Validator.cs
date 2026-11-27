using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Validators
{
    public class AnnulInventoryEntryValidator : BaseRequestValidator<AnnulInventoryEntryCommand>
    {
        private const int MinReasonLength = 5;
        private const int MaxReasonLength = 500;

        public AnnulInventoryEntryValidator()
        {
            RuleFor(x => x.InventoryEntryId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la entrada de inventario es obligatorio.");

            RuleFor(x => x.Reason)
                .Cascade(CascadeMode.Stop)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("El motivo de anulación es obligatorio.")
                .Must(value => value.Trim().Length >= MinReasonLength)
                .WithMessage($"El motivo de anulación debe contener al menos {MinReasonLength} caracteres.")
                .MaximumLength(MaxReasonLength)
                .WithMessage($"El motivo de anulación no puede superar los {MaxReasonLength} caracteres.");
        }
    }
}
