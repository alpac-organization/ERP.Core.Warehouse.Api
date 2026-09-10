using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class GetLotCapacitiesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper)
         : BaseValidatorHandler<GetLotCapacitiesQuery, LotCapacitiesDto>(_unitOfWork, _errorManager)
   {
      public override async Task<LotCapacitiesDto> Handle(GetLotCapacitiesQuery request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
         if (!access.IsSuccess) { return access.ErrorResponse!; }

         var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(
               s => s.Id == request.SectionId && s.IsActive && s.DeletedAt == null,
               cancellationToken);

         if (section is null)
            return _errorManager.ThrowBadRequest<LotCapacitiesDto>
               ("La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

         if (section.WarehouseId != request.WarehouseId)
            return _errorManager.ThrowBadRequest<LotCapacitiesDto>
               ("La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH");

         var lot = await _unitOfWork.Lots.Entities
            .AsNoTracking()
            .Include(l => l.LotsCapacity)
            .FirstOrDefaultAsync(
               l => l.Id == request.LotId
                  && l.SectionId == request.SectionId
                  && l.DeletedAt == null,
               cancellationToken);

         if (lot is null)
            return _errorManager.ThrowBadRequest<LotCapacitiesDto>
               ("El tramo no fue encontrado.", "ERP:LOT_NOT_FOUND");

         if (lot.LotsCapacity is null)
            return _errorManager.ThrowBadRequest<LotCapacitiesDto>
               ("No se encontró la capacidad del tramo.", "ERP:LOT_CAPACITY_NOT_FOUND");

         return _mapper.Map<LotCapacitiesDto>(lot.LotsCapacity);
      }
   }
}