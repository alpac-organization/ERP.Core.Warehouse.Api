using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators
{
    public class RegisterWarehouseValidator : AbstractValidator<RegisterWarehouseCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        public RegisterWarehouseValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código del almacén es obligatorio.")
                .MaximumLength(20).WithMessage("El código del almacén no puede superar los 20 caracteres.")
                .MustAsync(BeUniqueCodeAsync).WithMessage("El codigo de bodega debe ser unico & ya existe");

            RuleFor(x => x.WarehouseType)
                .IsInEnum().WithMessage("El tipo de almacén no es válido.");
            
            RuleFor(x=> x.Width)
                .GreaterThan(0)
                .WithMessage("El ancho debe ser mayor a cero")
                .PrecisionScale(18,2, ignoreTrailingZeros:true);
            
            RuleFor(x=> x.Length)
                .GreaterThan(0)
                .WithMessage("El largo debe ser mayor a cero")
                .PrecisionScale(18,2, ignoreTrailingZeros:true);

            RuleFor(x=> x.WarehouseLocation)
                .NotNull()
                .WithMessage("La ubicación del almacén es obligatoria");

            RuleFor(x=> x.WarehouseLocation.LocationName)
                .NotEmpty()
                .WithMessage("El nombre de la ubicación del almacén es obligatoria");

            RuleFor(x=> x.MaximumHeight)
                .GreaterThan(0)
                .WithMessage("La altura maxima debe mayor a cero")
                .PrecisionScale(18,2, ignoreTrailingZeros:true);

            RuleFor(x=> x.MinimumHeight)
                .GreaterThanOrEqualTo(0)
                .WithMessage("La altura minima no puede ser negativo")
                .PrecisionScale(18,2, ignoreTrailingZeros:true); 

            RuleFor(x=> x.MarginTop)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El margen superior no puede ser negativo")
                .PrecisionScale(18,2, ignoreTrailingZeros:true); 

            RuleFor(x=> x.MarginBottom)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El margen inferior no puede ser negativo")
                .PrecisionScale(18,2, ignoreTrailingZeros:true); 

            RuleFor(x=> x.MarginLeft)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El margen izquierdo no puede ser negativo")
                .PrecisionScale(18,2, ignoreTrailingZeros:true); 
 
            RuleFor(x=> x.MarginRight)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El margen Derecho no puede ser negativo")
                .PrecisionScale(18,2, ignoreTrailingZeros:true); 
            
        }
        public async Task<bool> BeUniqueCodeAsync(string code, CancellationToken cancellationToken)
        {
            var existsCode = await _unitOfWork.Warehouses.Entities.AnyAsync(w=> w.Code == code, cancellationToken);
            return !existsCode;
        }
    }
}