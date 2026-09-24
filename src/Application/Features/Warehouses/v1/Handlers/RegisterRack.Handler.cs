using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterRacksBulkHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IRackCapacityCalculator capacityCalculator,
    ICodeGenerator codeGenerator,
    ILogger<RegisterRacksBulkHandler> logger)
    : BaseRacksCapacityHandler<RegisterRacksBulkCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(RegisterRacksBulkCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando creación masiva de {Quantity} racks en la sección {SectionId}.", request.Quantity, request.SectionId);

        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId, request.CompanyId, request.ModuleCode, request.SectionId, request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        if (section!.SectionType == SectionType.Aisle)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden crear racks en una sección de tipo pasillo.",
                "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_RACKS");

        if (section.SectionStorageType != SectionStorageType.Racks)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta sección no admite racks (tipo de almacenamiento no coincide).",
                "ERP:SECTION_STORAGE_MISMATCH");

        var (codesAreValid, codes) = await codeGenerator.GenerateUniqueStorageCodesAsync(
            StorageEntityType.Rack, section.Id, request.Quantity, cancellationToken);
        if (!codesAreValid)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pudo generar la secuencia de códigos para los racks.",
                "ERP:RACK_CODE_GENERATION_FAILED");

        CalculateRackResult? lastCalc = null;

        for (int i = 0; i < request.Quantity; i++)
        {
            var code = codes[i];

            // 1. Entidad Racks
            var rack = RackProfile.ToRackEntity(
                section.Id,
                code,
                request.RowNumber,
                request.LevelNumber,
                request.MaxPulleys,
                request.UsageProfile);

            await _unitOfWork.Racks.RegisterRack(rack);

            // 2. Entidades RackPositions (Polines correspondientes al nivel del rack)
            for (int col = 1; col <= request.MaxPulleys; col++)
            {
                var posCode = $"{code}-N{request.LevelNumber}P{col}";
                var position = RackProfile.ToRackPositionEntity(
                    rack.Id,
                    posCode,
                    request.RowNumber,
                    col,
                    request.LevelNumber);

                await _unitOfWork.RackPositions.RegisterRackPosition(position);
            }

            // 3. Entidad RacksCoordinates
            var posX = request.InitialPositionX + (i * request.SpacingX);
            var posY = request.InitialPositionY;
            var coords = RackProfile.ToRackCoordinatesEntity(rack.Id, posX, posY);
            await _unitOfWork.RackCoordinates.RegisterRackCoordinate(coords);

            // 4. Capacidad del Rack y recálculo en cascada
            lastCalc = await capacityCalculator.CalculateRackAsync(
                section.Id,
                request.Width,
                request.Length,
                request.Height,
                cancellationToken);

            if (lastCalc.Rack != null)
            {
                lastCalc.Rack.RackId = rack.Id;
                await _unitOfWork.RackCapacities.RegisterRackCapacity(lastCalc.Rack);
            }
        }

        // 5. Aplicar nueva capacidad en cascada a Section y Warehouse
        if (lastCalc != null)
        {
            await ApplySectionCapacityAsync(section, lastCalc.Section, cancellationToken);
            await ApplyWarehouseCapacityAsync(section.WarehouseId, lastCalc.Warehouse, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Creación masiva de {Quantity} racks exitosa.", request.Quantity);

        return true;
    }
}
