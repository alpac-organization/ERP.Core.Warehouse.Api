using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class GetLotCoordinatesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper)
      : BaseValidatorHandler<GetLotCoordinatesQuery, LotCoordinatesDto>(_unitOfWork, _errorManager)
   {
      public override async Task<LotCoordinatesDto> Handle(GetLotCoordinatesQuery request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
         if (!access.IsSuccess) { return access.ErrorResponse!; }

         var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(
               s => s.Id == request.SectionId && s.IsActive && s.DeletedAt == null,
               cancellationToken);

         if (section is null)
            return _errorManager.ThrowBadRequest<LotCoordinatesDto>
               ("La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

         if (section.WarehouseId != request.WarehouseId)
            return _errorManager.ThrowBadRequest<LotCoordinatesDto>
               ("La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH");

         var lot = await _unitOfWork.Lots.Entities
            .AsNoTracking()
            .Include(l => l.LotsCoordinates)
            .FirstOrDefaultAsync(
               l => l.Id == request.LotId
                  && l.SectionId == request.SectionId
                  && l.DeletedAt == null,
               cancellationToken);

         if (lot is null)
            return _errorManager.ThrowBadRequest<LotCoordinatesDto>
               ("El tramo no fue encontrado.", "ERP:LOT_NOT_FOUND");

         if (lot.LotsCoordinates is null)
            return new LotCoordinatesDto { LotId = lot.Id };

         return _mapper.Map<LotCoordinatesDto>(lot.LotsCoordinates);
      }
   }
}