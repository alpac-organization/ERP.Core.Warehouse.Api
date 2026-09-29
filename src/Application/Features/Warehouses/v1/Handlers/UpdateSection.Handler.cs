using AutoMapper;
using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class UpdateSectionHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper,
        ISectionCapacityCalculator sectionCapacityCalculator,
        ILogger<UpdateSectionHandler> logger)
        : BaseSectionCapacityHandler<UpdateSectionCommand>(unitOfWork, errorManager, mapper, sectionCapacityCalculator)
    {
        public override async Task<bool> Handle(UpdateSectionCommand request, CancellationToken cancellationToken)
        {
            var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
                request.UserId,
                request.CompanyId,
                request.ModuleCode,
                request.SectionId,
                request.WarehouseId,
                cancellationToken);

            if (!isValid) return errorResponse;

            logger.LogInformation("🚩Iniciando proceso de actualización de sección.");

            if (request.Width.HasValue || request.Length.HasValue)
            {
                var (capacityOk, capacityError) = await RecalculateSectionAndWarehouseCapacityAsync(
                    section!,
                    request.WarehouseId,
                    request.SectionId,
                    request.Width,
                    request.Length,
                    cancellationToken);

                if (!capacityOk) return capacityError;
            }

            await _unitOfWork.Sections.UpdateAsync(section!);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Sección de Almacén actualizada con éxito✅");

            return true;
        }
    }
}
