using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
   public class GetLotLayoutValidator : BaseRequestValidator<GetLotLayoutQuery>
   {
      public GetLotLayoutValidator()
      {
         RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El id del almacén es requerido.");

         RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la sección no es válido.");
      }
   }
}
