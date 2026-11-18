using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Validators
{
    public class GetInventoryEntryDetailsValidator : BaseRequestValidator<GetInventoryEntryDetailsQuery>
    {
        public GetInventoryEntryDetailsValidator()
        {
            RuleFor(x => x.InventoryEntryId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la entrada de inventario es requerido.");
        }
    }
}
