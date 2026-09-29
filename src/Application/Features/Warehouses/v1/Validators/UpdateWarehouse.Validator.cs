using System.Security.Cryptography.X509Certificates;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using FluentValidation;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
   public class UpdateWarehouseValidator: AbstractValidator<UpdateWarehouseCommand>
   {
      public UpdateWarehouseValidator()
      {
         RuleFor(x=>x.WarehouseId)
            .NotEmpty()
            .WithMessage("El id de warehouse es requerido")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de warehouse no es valido");

         RuleFor(x=>x)
            .Must((x)=> x.Code is not null 
                  || x.IsActive.HasValue 
                  || x.WarehouseType.HasValue
                  || x.Location is not null
                  || x.Capacity is not null)
            .WithMessage("Debe enviar al menos un campo para actualizar ");
         
         RuleFor(x=>x.Code)
            .NotEmpty()
            .WithMessage("El codigo de warehouse es requerido")
            .MaximumLength(30)
            .WithMessage("El codigo de warehouse no puede superar lo establecido")
            .When(x=>x.Code is not null);

         RuleFor(x=>x.WarehouseType)
            .IsInEnum()
            .WithMessage("El tipo de warehouse no es valido")
            .When(x=>x.WarehouseType.HasValue);

         When(x=>x.Location is not null, ()=>
         {
            RuleFor(x=>x.Location!.LocationName)
            .NotEmpty()
            .WithMessage("El nombre de la ubicacion de warehouse no debe estar vacio")
            .When(x=>x.Location!.LocationName is not null);
         });

         When(x=>x.Capacity is not null, () =>
         {
            RuleFor(x=>x.Capacity!.Width)
               .GreaterThan(0)
               .WithMessage("EL ancho debe ser mayor a cero")
               .PrecisionScale(18,2, ignoreTrailingZeros:true)
               .When(x=>x.Capacity!.Width.HasValue);

            RuleFor(x=>x.Capacity!.Length)
               .GreaterThan(0)
               .WithMessage("La longitud debe ser mayor a cero")
               .PrecisionScale(18,2, ignoreTrailingZeros:true)
               .When(x=>x.Capacity!.Width.HasValue);

            RuleFor(x=>x.Capacity!.MaximumHeight)
               .GreaterThan(0)
               .WithMessage("La altura maxima debe ser mayor a cero")
               .PrecisionScale(18,2, ignoreTrailingZeros:true)
               .When(x=>x.Capacity!.MaximumHeight.HasValue);

            RuleFor(x=>x.Capacity!.MinimumHeight)
               .GreaterThanOrEqualTo(0)
               .WithMessage("La altura minima debe ser mayor o igual a cero")
               .PrecisionScale(18,2, ignoreTrailingZeros:true)
               .When(x=>x.Capacity!.MinimumHeight.HasValue);

            RuleFor(x=>x.Capacity!)
               .Must(x=>!x.MinimumHeight.HasValue 
                     || !x.MaximumHeight.HasValue
                     || x.MaximumHeight >= x.MinimumHeight)
               .WithMessage("La altura máxima debe ser mayor o igual a la altura mínima.");
         });
      }
   }
}