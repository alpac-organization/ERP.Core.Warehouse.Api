using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class UpdateSectionLayoutHandler(
      IUnitOfWork unitOfWork,
      IErrorManager errorManager,
      IMapper mapper,
      ISectionCapacityCalculator sectionCapacityCalculator,
      ILogger<UpdateSectionLayoutHandler> logger)
      : BaseSectionCapacityHandler<UpdateSectionLayoutCommand>(unitOfWork, errorManager, mapper)
   {
      public override async Task<bool> Handle(UpdateSectionLayoutCommand request, CancellationToken cancellationToken)
      {
         var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            request.SectionId,
            request.WarehouseId,
            cancellationToken,
            includeCoordinates: true);

         if (!isValid) return errorResponse;

         logger.LogInformation("🚩Iniciando proceso de actualización de layout de sección.");

         if (section!.SectionCoordinates is null)
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

            var sectionCapacityCalculation = await sectionCapacityCalculator.UpdateSectionAsync(
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

         logger.LogInformation("Layout de sección de almacén actualizada con éxito✅");

         return true;
      }
   }
}
