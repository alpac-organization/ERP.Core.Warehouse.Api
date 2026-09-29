using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class UpdateWarehouseHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager,ILogger<UpdateWarehouseHandler> _logger)
   :BaseValidatorHandler<UpdateWarehouseCommand,bool>(_unitOfWork, _errorManager)
   {
     public override async Task<bool> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
      {
        var access = await ValidateAccessAsync(request.UserId,request.CompanyId,request.ModuleCode,cancellationToken);

         if (!access.IsSuccess)
         {
            return access.ErrorResponse;
         }

        if(access.Role?.RoleType is RoleType.Operator or RoleType.Supervisor)
         {
            return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
         }

         _logger.LogInformation("🚩 Iniciando Actualizacion de warehouse");

         var warehouseDetails = await _unitOfWork.Warehouses.Entities
                                .Include(w=>w.WarehouseCapacity)
                                .Include(w=>w.WarehouseLocation)
                                .Where(w=>w.IsActive)
                                .Where(w=>w.Id == request.WarehouseId)
                                .FirstOrDefaultAsync(cancellationToken);

         if (warehouseDetails is null)
         {
            return _errorManager.ThrowNotFound<bool>("La warehouse no existe.", "ERP:WAREHOUSE_NOT_FOUND");
         }

         return true; 
      } 
   } 
}