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

        var secWidth = section.SectionCapacity?.Width ?? 0m;
        var secLength = section.SectionCapacity?.Length ?? 0m;
        var isVertical = secLength >= secWidth;

        var rotationY = request.RotationY ?? (isVertical ? 90m : 0m);
        var isRotated90 = Math.Abs(rotationY - 90m) < 0.01m || Math.Abs(rotationY - 270m) < 0.01m;

        if (section.SectionCapacity == null || secWidth <= 0 || secLength <= 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene dimensiones físicas válidas (ancho y largo) configuradas. Debe definir la capacidad de la sección antes de crear racks.",
                "ERP:SECTION_CAPACITY_NOT_CONFIGURED");
        }

        if (request.Quantity > 1 && request.SpacingX < request.Length)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"La separación entre racks ({request.SpacingX:F2}m) no puede ser menor que la longitud del rack ({request.Length:F2}m) para evitar colisiones internas.",
                "ERP:RACK_SPACING_LESS_THAN_LENGTH");
        }

        var rackDimX = isRotated90 ? request.Width : request.Length;
        var rackDimY = isRotated90 ? request.Length : request.Width;

        decimal endX;
        decimal endY;

        if (isRotated90)
        {
            // Pasillo vertical: la hilera es X, los racks avanzan a lo largo de Y
            endX = request.InitialPositionX + rackDimX;
            endY = request.InitialPositionY + ((request.Quantity - 1) * request.SpacingX) + rackDimY;
        }
        else
        {
            // Pasillo horizontal: los racks avanzan a lo largo de X, la hilera es Y
            endX = request.InitialPositionX + ((request.Quantity - 1) * request.SpacingX) + rackDimX;
            endY = request.InitialPositionY + rackDimY;
        }

        if (request.InitialPositionX < 0 || request.InitialPositionY < 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Las coordenadas iniciales del rack no pueden ser negativas.",
                "ERP:RACK_COORDINATES_OUT_OF_BOUNDS");
        }

        if (endX > secWidth)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"La posición de los racks excede el ancho horizontal (X) de la sección (requiere {endX:F2}m, disponible: {secWidth:F2}m).",
                "ERP:RACK_EXCEEDS_SECTION_WIDTH");
        }

        if (endY > secLength)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"La distribución de los {request.Quantity} racks excede el largo vertical (Y) de la sección (requiere {endY:F2}m, disponible: {secLength:F2}m).",
                "ERP:RACK_EXCEEDS_SECTION_LENGTH");
        }

        // Validación de colisión física con racks existentes en el mismo nivel
        var existingRacks = await _unitOfWork.Racks.Entities
            .Include(r => r.RacksCoordinates)
            .Include(r => r.RackCapacity)
            .Where(r => r.SectionId == section.Id
                     && r.LevelNumber == request.LevelNumber
                     && r.DeletedAt == null)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < request.Quantity; i++)
        {
            decimal posX = isRotated90 ? request.InitialPositionX : request.InitialPositionX + (i * request.SpacingX);
            decimal posY = isRotated90 ? request.InitialPositionY + (i * request.SpacingX) : request.InitialPositionY;
            decimal newX1 = posX;
            decimal newX2 = posX + rackDimX;
            decimal newY1 = posY;
            decimal newY2 = posY + rackDimY;

            foreach (var existing in existingRacks)
            {
                if (existing.RacksCoordinates == null) continue;
                var exX = existing.RacksCoordinates.PositionX;
                var exY = existing.RacksCoordinates.PositionY;
                var exRot = existing.RacksCoordinates.RotationY;
                var exIsRot = Math.Abs(exRot - 90m) < 0.01m || Math.Abs(exRot - 270m) < 0.01m;
                var exW = existing.RackCapacity?.Width ?? 1.07m;
                var exL = existing.RackCapacity?.Length ?? 2.44m;
                var exDimX = exIsRot ? exW : exL;
                var exDimY = exIsRot ? exL : exW;
                var exX1 = exX;
                var exX2 = exX + exDimX;
                var exY1 = exY;
                var exY2 = exY + exDimY;

                if ((newX1 + 0.02m < exX2) && (newX2 - 0.02m > exX1) &&
                    (newY1 + 0.02m < exY2) && (newY2 - 0.02m > exY1))
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        $"El rack #{i + 1} en posición ({posX:F2}m, {posY:F2}m) colisiona físicamente con el rack existente '{existing.Code}' en el nivel {request.LevelNumber}.",
                        "ERP:RACK_PHYSICAL_COLLISION");
                }
            }
        }

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
            decimal posX;
            decimal posY;

            if (isRotated90)
            {
                // Pasillo vertical: X es la hilera, Y avanza a lo largo del pasillo
                posX = request.InitialPositionX;
                posY = request.InitialPositionY + (i * request.SpacingX);
            }
            else
            {
                // Pasillo horizontal: X avanza a lo largo del pasillo, Y es la hilera
                posX = request.InitialPositionX + (i * request.SpacingX);
                posY = request.InitialPositionY;
            }

            var coords = RackProfile.ToRackCoordinatesEntity(rack.Id, posX, posY, 0m, rotationY);
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
