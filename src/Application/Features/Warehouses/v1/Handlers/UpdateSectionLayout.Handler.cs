using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class UpdateSectionLayoutHandler(
      IUnitOfWork unitOfWork,
      IErrorManager errorManager,
      IMapper mapper,
      ISectionCapacityCalculator sectionCapacityCalculator,
      ILogger<UpdateSectionLayoutHandler> logger)
      : BaseSectionCapacityHandler<UpdateSectionLayoutCommand>(unitOfWork, errorManager, mapper, sectionCapacityCalculator)
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

         var activeSection = section!;

         var shouldUpdateCoordinates = request.PositionX.HasValue || request.PositionY.HasValue ||
                                       request.PositionZ.HasValue || request.RotationY.HasValue;

         if (shouldUpdateCoordinates)
         {
            if (request.PositionX.HasValue) activeSection.SectionCoordinates.PositionX = request.PositionX.Value;

            if (request.PositionY.HasValue) activeSection.SectionCoordinates.PositionY = request.PositionY.Value;

            if (request.PositionZ.HasValue) activeSection.SectionCoordinates.PositionZ = request.PositionZ.Value;

            if (request.RotationY.HasValue) activeSection.SectionCoordinates.RotationY = request.RotationY.Value;

            await _unitOfWork.SectionCoordinates.UpdateAsync(activeSection.SectionCoordinates);
         }

         if (request.Width.HasValue || request.Length.HasValue)
         {
            var (capacityOk, capacityError) = await RecalculateSectionAndWarehouseCapacityAsync(
               activeSection,
               request.WarehouseId,
               request.SectionId,
               request.Width,
               request.Length,
               cancellationToken);

            if (!capacityOk) return capacityError;
         }

         await _unitOfWork.SaveChangesAsync(cancellationToken);

         logger.LogInformation("Layout de sección de almacén actualizada con éxito✅");

         return true;
      }
   }
}
