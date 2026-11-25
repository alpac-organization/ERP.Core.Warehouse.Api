using FluentValidation;
using ERP.Core.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Validators
{
    public class GetKardexMovementValidator : BaseRequestValidator<GetKardexMovementQuery>
    {
        public GetKardexMovementValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEqual(Guid.Empty)
                .When(x => x.BranchId.HasValue)
                .WithMessage("El identificador de la sucursal no es un Guid válido.");

            RuleFor(x => x.CostCenterId)
                .NotEqual(Guid.Empty)
                .When(x => x.CostCenterId.HasValue)
                .WithMessage("El identificador del centro de costo no es un Guid válido.");

            RuleFor(x => x.WorkAreaId)
                .NotEqual(Guid.Empty)
                .When(x => x.WorkAreaId.HasValue)
                .WithMessage("El identificador del área de trabajo no es un Guid válido.");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithMessage("El identificador de la categoría no es un Guid válido.");

            RuleFor(x => x.KardexMovementType)
                .IsInEnum()
                .When(x => x.KardexMovementType.HasValue)
                .WithMessage("El tipo de movimiento del kardex no es válido.");

            RuleFor(x => x.KardexType)
                .IsInEnum()
                .When(x => x.KardexType.HasValue)
                .WithMessage("El tipo de kardex no es válido.");

            RuleFor(x => x.DateTo)
                .GreaterThanOrEqualTo(x => x.DateFrom)
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
                .WithMessage("La fecha desde no puede ser mayor que la fecha hasta.");
        }
    }
}
