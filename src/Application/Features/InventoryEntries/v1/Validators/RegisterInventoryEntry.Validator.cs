using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Validators
{
    public class RegisterInventoryEntryValidator : BaseRequestValidator<RegisterInventoryEntryCommand>
    {
        public RegisterInventoryEntryValidator()
        {
            RuleFor(x => x.EntryType)
                .IsInEnum()
                .WithMessage("el tipo de entrada de inventario es requerida");

            RuleFor(x => x.EntryType)
                .IsInEnum()
                    .WithMessage("El tipo de entrada de inventario no es válido.")
                .Equal(EntryType.Buy)
                    .WithMessage("El tipo de entrada debe ser Compra.");
        }
    }
}
