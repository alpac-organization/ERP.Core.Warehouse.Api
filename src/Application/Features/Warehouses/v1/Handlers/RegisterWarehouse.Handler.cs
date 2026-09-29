using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class RegisterWarehouseHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager
    ,IWarehouseCapacityCalculator warehouseCapacityCalculator, ILogger<RegisterWarehouseHandler> _logger)
        :  BaseValidatorHandler<RegisterWarehouseCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(RegisterWarehouseCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType is RoleType.Supervisor or  RoleType.Operator)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
            }
            
            _logger.LogInformation("🚀Iniciando proceso de registro de almacen.");

            var calculationWarehouseCapacity = await warehouseCapacityCalculator.CalculateWarehouseAsync(
                request.Width,
                request.Length,
                request.HasMargins,
                request.MinimumHeight,
                request.MaximumHeight,
                request.MarginTop,
                request.MarginBottom,
                request.MarginRight,
                request.MarginLeft,
                cancellationToken
            );

            if(calculationWarehouseCapacity.Warehouse is null)
            {
                return _errorManager.ThrowBadRequest<bool>( "No se pudo calcular la capacidad del almacén.",
                "ERP:01");
            }


            var warehouse   = request.ToWarehouseEntity();
            var capacity = request.ToWarehouseCapacityEntity(warehouse.Id,calculationWarehouseCapacity.Warehouse);
            var location = request.WarehouseLocation.ToWarehouseLocation(request.CompanyId,warehouse.Id);

            
            await _unitOfWork.Warehouses.RegisterWarehouse(warehouse);
            await _unitOfWork.WarehouseCapacities.RegisterWarehouseCapacity(capacity);
            await _unitOfWork.Locations.RegisterLocation(location);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Registro de almacen correctamente.");
            return true;
        }
    }
}