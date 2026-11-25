using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Validators
{
    public class GetKardexMovementDetailsValidator : BaseRequestValidator<GetKardexMovementDetailsQuery>
    {
        public GetKardexMovementDetailsValidator()
        {
            RuleFor(x => x.KardexId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador del movimiento de kardex es obligatorio.");
        }
    }
}
