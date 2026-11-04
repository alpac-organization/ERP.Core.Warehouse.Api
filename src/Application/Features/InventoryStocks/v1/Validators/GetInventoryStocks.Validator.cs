using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Validators
{
    public class GetInventoryStocksValidator : BaseRequestValidator<GetInventoryStocksQuery>
    {
        public GetInventoryStocksValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithMessage("El identificador de la categoría no es un Guid válido.");
        }
    }
    
}
