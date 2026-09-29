using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class DeleteSectionHandler(
      IUnitOfWork _unitOfWork,
      IErrorManager _erroManager,
      IMapper _mapper,
      ISectionCapacityCalculator _sectionCapacityCalculator,
      ILogger<DeleteSectionHandler> _logger)
      : BaseValidatorHandler<DeleteSectionCommand, bool>(_unitOfWork, _erroManager)
   {
      public override async Task<bool> Handle(DeleteSectionCommand request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

         if (!access.IsSuccess)
         {
            return access.ErrorResponse;
         }

         if (access.Role?.RoleType != RoleType.Administrator)
         {
            return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
         }

         _logger.LogInformation("🚩Iniciando proceso de eliminación de sección.");

         var section = await _unitOfWork.Sections.Entities
            .Include(s => s.SectionCapacity)
            .FirstOrDefaultAsync(s =>
               s.Id == request.SectionId &&
               s.WarehouseId == request.WarehouseId &&
               s.DeletedAt == null,
               cancellationToken);

         if (section is null)
         {
            return _errorManager.ThrowBadRequest<bool>("No se encontró la sección a eliminar", "ERP:NOT_FOUND");
         }

         var hasLots = await _unitOfWork.Lots.Entities
            .AnyAsync(l => l.SectionId == section.Id && l.DeletedAt == null, cancellationToken);

         var hasRacks = await _unitOfWork.Racks.Entities
            .AnyAsync(r => r.SectionId == section.Id && r.DeletedAt == null, cancellationToken);

         if (hasLots || hasRacks)
         {
            return _errorManager.ThrowBadRequest<bool>(
               "No se puede eliminar la sección porque aún tiene tramos o racks activos. Muévelos a otra sección o elimínalos antes de continuar.",
               "ERP:SECTION_HAS_CHILDREN");
         }

         var capacityCalculation = await _sectionCapacityCalculator.DeleteSectionAsync(request.SectionId, cancellationToken);

         if (capacityCalculation.Warehouse is null)
         {
            return _errorManager.ThrowBadRequest<bool>(
               "No se pudo recalcular la capacidad del almacén al eliminar la sección.",
               "ERP:01");
         }

         var warehouseCapacity = await _unitOfWork.WarehouseCapacities.Entities
            .FirstOrDefaultAsync(c => c.WarehouseId == request.WarehouseId, cancellationToken);

         if (warehouseCapacity is null)
         {
            return _errorManager.ThrowBadRequest<bool>("El almacén no tiene capacidad registrada", "ERP:01");
         }

         _mapper.Map(capacityCalculation.Warehouse, warehouseCapacity);
         await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouseCapacity);

         if (section.SectionCapacity is not null)
         {
            section.SectionCapacity.DeletedAt = NicaraguaClock.Now;
            await _unitOfWork.SectionCapacities.UpdateAsync(section.SectionCapacity);
         }

         section.IsActive = false;
         section.DeletedAt = NicaraguaClock.Now;

         await _unitOfWork.Sections.UpdateAsync(section);
         await _unitOfWork.SaveChangesAsync(cancellationToken);

         _logger.LogInformation("Sección de almacén eliminada con éxito✅");

         return true;
      }
   }
}
