using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   /// <summary>
   /// Entrega en una sola llamada todo lo que el plano 2D de la seccion necesita:
   /// la forma real de la seccion y todos sus tramos con capacidad y coordenadas.
   /// Evita el fan-out de una request por tramo que hacia el frontend.
   /// </summary>
   public class GetLotLayoutHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper)
      : BaseValidatorHandler<GetLotLayoutQuery, LotLayoutDto>(_unitOfWork, _errorManager)
   {
      public override async Task<LotLayoutDto> Handle(GetLotLayoutQuery request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
         if (!access.IsSuccess) { return access.ErrorResponse!; }

         var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .Include(sec => sec.SectionCapacity)
            .Include(sec => sec.SectionCoordinates)
            .FirstOrDefaultAsync(
               sec => sec.Id == request.SectionId && sec.IsActive && sec.DeletedAt == null,
               cancellationToken);

         if (section is null)
            return _errorManager.ThrowBadRequest<LotLayoutDto>
               ("La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

         if (section.WarehouseId != request.WarehouseId)
            return _errorManager.ThrowBadRequest<LotLayoutDto>
               ("La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH");

         var lots = await _unitOfWork.Lots.Entities
            .AsNoTracking()
            .Include(l => l.LotsCapacity)
            .Include(l => l.LotsCoordinates)
            .Where(l => l.SectionId == request.SectionId && l.DeletedAt == null)
            .OrderBy(l => l.Code)
            .ToListAsync(cancellationToken);

         var layout = _mapper.Map<LotLayoutDto>(section);
         layout.Lots = _mapper.Map<List<LotLayoutItemDto>>(lots);

         return layout;
      }
   }
}
