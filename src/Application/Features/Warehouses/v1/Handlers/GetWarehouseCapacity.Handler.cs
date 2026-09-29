using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class GetWarehouseCapacityHandler(IUnitOfWork unitOfWork,IErrorManager errorManager, IMapper mapper)
   :BaseValidatorHandler<GetWarehouseCapacitiesQuery,WarehouseCapacitiesDto>(unitOfWork,errorManager)
   {
     public override async Task<WarehouseCapacitiesDto> Handle(
         GetWarehouseCapacitiesQuery request,
         CancellationToken cancellationToken
     )
      {
         var access = await ValidateAccessAsync(request.UserId,request.CompanyId,request.ModuleCode, cancellationToken);
         if(!access.IsSuccess)
         {
            return access.ErrorResponse!;
         }

         var warehouse = await _unitOfWork.Warehouses.Entities
                        .AsNoTracking()
                        .Include(w=>w.WarehouseCapacity)
                        .FirstOrDefaultAsync(w=> w.Id == request.WarehouseId && w.DeletedAt == null, cancellationToken);
         
         if(warehouse is null)
         {
            return _errorManager.ThrowBadRequest<WarehouseCapacitiesDto>("El almacén indicado no existe.", "ERP:WAREHOUSE_NOT_FOUND");
         }
         if (warehouse.WarehouseCapacity is null)
         {
            return _errorManager.ThrowBadRequest<WarehouseCapacitiesDto>("No se encontró la capacidad del almacén.", "ERP:WAREHOUSE_CAPACITY_NOT_FOUND");
         }
      
         return mapper.Map<WarehouseCapacitiesDto>(warehouse.WarehouseCapacity);
      }
   }
}
