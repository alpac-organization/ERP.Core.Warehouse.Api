using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class UpdateSectionLayoutHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper, ISectionCapacityCalculator _sectionCapacityCalculator, ILogger<UpdateSectionLayoutHandler> _logger) : BaseValidatorHandler<UpdateSectionLayoutCommand, bool>(_unitOfWork, _errorManager)
   {
      public override async Task<bool> Handle(UpdateSectionLayoutCommand request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

         if (!access.IsSuccess) return access.ErrorResponse;

         if (access.Role?.RoleType != RoleType.Administrator)
         {
            return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
         }

         _logger.LogInformation("🚩Iniciando proceso de actualización de layout de sección.");

         var section = await _unitOfWork.Sections.Entities
            .Include(s => s.SectionCoordinates)
            .Include(s => s.SectionCapacity)
            .FirstOrDefaultAsync(s =>
               s.Id == request.SectionId &&
               s.WarehouseId == request.WarehouseId &&
               s.DeletedAt == null &&
               s.IsActive,
               cancellationToken);

         if (section is null)
         {
            return _errorManager.ThrowBadRequest<bool>("La sección indicada no existe, está inactiva o no pertenece al almacén.", "ERP:01");
         }

         var warehouseExists = await _unitOfWork.Warehouses.Entities
            .AnyAsync(w =>
               w.Id == request.WarehouseId &&
               w.IsActive &&
               w.DeletedAt == null,
               cancellationToken);

         if (!warehouseExists)
         {
            return _errorManager.ThrowBadRequest<bool>("El almacén indicado no existe o no está activo.", "ERP:01");
         }

         if (section.SectionCoordinates is null)
         {
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene coordenadas registradas. Regístralas antes de actualizar el layout.",
                "ERP:SECTION_COORDINATES_NOT_FOUND");
         }

         var shouldUpdateCoordinates = request.PositionX.HasValue || request.PositionY.HasValue ||
                                       request.PositionZ.HasValue || request.RotationY.HasValue;

         if (shouldUpdateCoordinates)
         {
            if (request.PositionX.HasValue) section.SectionCoordinates.PositionX = request.PositionX.Value;

            if (request.PositionY.HasValue) section.SectionCoordinates.PositionY = request.PositionY.Value;

            if (request.PositionZ.HasValue) section.SectionCoordinates.PositionZ = request.PositionZ.Value;

            if (request.RotationY.HasValue) section.SectionCoordinates.RotationY = request.RotationY.Value;

            await _unitOfWork.SectionCoordinates.UpdateAsync(section.SectionCoordinates);
         }

         var shouldRecalculateCapacity = request.Width.HasValue || request.Length.HasValue;

         if (shouldRecalculateCapacity)
         {
            var sectionCapacity = section.SectionCapacity;
            var warehouseCapacity = await _unitOfWork.WarehouseCapacities.Entities
               .FirstOrDefaultAsync(c => c.WarehouseId == request.WarehouseId, cancellationToken);

            if (warehouseCapacity is null)
            {
               return _errorManager.ThrowBadRequest<bool>("El almacén no tiene capacidad registrada", "ERP:01");
            }

            if (sectionCapacity is null)
            {
               return _errorManager.ThrowBadRequest<bool>("La sección no tiene capacidad registrada.", "ERP:01");
            }

            var width = request.Width ?? sectionCapacity.Width;
            var length = request.Length ?? sectionCapacity.Length;

            var sectionCapacityCalculation = await _sectionCapacityCalculator.UpdateSectionAsync(
               request.SectionId, width, length, cancellationToken
            );

            if (sectionCapacityCalculation.Section is null || sectionCapacityCalculation.Warehouse is null)
            {
               return _errorManager.ThrowBadRequest<bool>("No se pudo calcular la capacidad de la sección.", "ERP:01");
            }

            _mapper.Map(sectionCapacityCalculation.Section, sectionCapacity);
            await _unitOfWork.SectionCapacities.UpdateAsync(sectionCapacity);

            _mapper.Map(sectionCapacityCalculation.Warehouse, warehouseCapacity);
            await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouseCapacity);
         }

         await _unitOfWork.SaveChangesAsync(cancellationToken);

         _logger.LogInformation("Layout de sección de almacén actualizada con éxito✅");

         return true;
      }
   }
}
