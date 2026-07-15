using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class RegisterWarehouseHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<RegisterWarehouseHandler> _logger) :  BaseValidatorHandler<RegisterWarehouseCommand, bool>(_unitOfWork, _errorManager)
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

            //Your code here
            var warehouseEntity = WarehouseMapper.ToWarehouseEntity(request);

            var lastCode = await _unitOfWork.Warehouses.Entities
                .Where(w => w.Code != null)
                .OrderByDescending(w => w.Code)
                .Select(w => w.Code)
                .FirstOrDefaultAsync(cancellationToken);

            warehouseEntity.Code = CodeGenerator.GenerateWarehouseCode(lastCode ?? "000001");
            
            await _unitOfWork.Warehouses.RegisterWarehouse(warehouseEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Registro de almacen correctamente.");
            return true;
        }
    }
}