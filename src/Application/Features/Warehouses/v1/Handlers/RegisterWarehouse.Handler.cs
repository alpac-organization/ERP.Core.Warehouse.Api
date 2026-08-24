using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class RegisterWarehouseHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<RegisterWarehouseHandler> _logger)
        :  BaseValidatorHandler<RegisterWarehouseCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(RegisterWarehouseCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
            }
            
            _logger.LogInformation("🚀Iniciando proceso de registro de almacen.");

            var warehouseEntity = request.ToWarehouseEntity();

            warehouseEntity.HasChildren = false;

            if (request.ParentWarehouseId.HasValue)
            {
                var parentId = request.ParentWarehouseId.Value;
                var parentWarehouse = await _unitOfWork.Warehouses.Entities
                    .FirstOrDefaultAsync(w => w.Id == parentId, cancellationToken);

                if (parentWarehouse == null)
                    return _errorManager.ThrowNotFound<bool>($"No se encontró el almacén padre con ID {parentId}", "ERP:02");

                if (!parentWarehouse.HasChildren)
                    parentWarehouse.HasChildren = true;
            }
            
            await _unitOfWork.Warehouses.RegisterWarehouse(warehouseEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Registro de almacen correctamente.");
            return true;
        }
    }
}