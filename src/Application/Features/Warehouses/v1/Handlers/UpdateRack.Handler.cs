using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class UpdateRackHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IRackCapacityCalculator capacityCalculator,
    ILogger<UpdateRackHandler> logger)
    : BaseRacksCapacityHandler<UpdateRackCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(UpdateRackCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando actualización integral del rack {RackId}.", request.RackId);

        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId, request.CompanyId, request.ModuleCode, request.SectionId, request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (rackCandidate, rackIsValid, rackError) = await GetExistingRackAsync(
            request.RackId, request.SectionId, cancellationToken);
        if (!rackIsValid) return rackError;

        var rack = rackCandidate!;

        // 1. Validación de capacidad y dimensiones de la sección
        if (section!.SectionCapacity == null || section.SectionCapacity.Width <= 0 || section.SectionCapacity.Length <= 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene dimensiones físicas válidas (ancho y largo) configuradas. Debe definir la capacidad de la sección antes de ubicar o redimensionar racks.",
                "ERP:SECTION_CAPACITY_NOT_CONFIGURED");
        }

        // 2. Validación de motivo obligatorio en caso de Bloqueo o Mantenimiento
        if (request.Status.HasValue &&
            (request.Status.Value == RackStatus.Blocked || request.Status.Value == RackStatus.UnderMaintenance))
        {
            var reason = !string.IsNullOrWhiteSpace(request.UnavailableReason)
                ? request.UnavailableReason
                : rack.UnavailableReason;

            if (string.IsNullOrWhiteSpace(reason))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe indicar el motivo de indisponibilidad al cambiar el estado a Bloqueado o En Mantenimiento.",
                    "ERP:RACK_UNAVAILABLE_REASON_REQUIRED");
            }
        }

        // 3. Validación de reducción de dimensiones con stock activo
        var currentWidth = rack.RackCapacity?.Width ?? 0m;
        var currentLength = rack.RackCapacity?.Length ?? 0m;

        bool isShrinking = (request.Width.HasValue && request.Width.Value < currentWidth) ||
                           (request.Length.HasValue && request.Length.Value < currentLength);

        if (isShrinking)
        {
            var positionIds = rack.Positions?
                .Where(p => p.DeletedAt == null)
                .Select(p => p.Id)
                .ToList() ?? [];

            if (positionIds.Count > 0)
            {
                var hasActiveStock = await _unitOfWork.StockPlacements.Entities
                    .AnyAsync(
                        s => s.RackPositionId != null
                            && positionIds.Contains(s.RackPositionId.Value)
                            && s.VacatedAtDate == null
                            && s.DeletedAt == null,
                        cancellationToken);

                if (hasActiveStock)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "No se pueden reducir las dimensiones del rack porque tiene stock activo asignado en sus posiciones.",
                        "ERP:RACK_CANNOT_SHRINK_WITH_STOCK");
                }
            }
        }

        // 4. Validación de límites y coordenadas en la sección
        var secWidth = section.SectionCapacity.Width;
        var secLength = section.SectionCapacity.Length;
        var isVertical = secLength >= secWidth;

        var targetPosX = request.PositionX ?? rack.RacksCoordinates?.PositionX ?? 0m;
        var targetPosY = request.PositionY ?? rack.RacksCoordinates?.PositionY ?? 0m;
        var targetRotY = request.RotationY ?? rack.RacksCoordinates?.RotationY ?? (isVertical ? 90m : 0m);
        var isRotated90 = Math.Abs(targetRotY - 90m) < 0.01m || Math.Abs(targetRotY - 270m) < 0.01m;

        var targetWidth = request.Width ?? rack.RackCapacity?.Width ?? 1.07m;
        var targetLength = request.Length ?? rack.RackCapacity?.Length ?? 2.44m;

        var dimX = isRotated90 ? targetWidth : targetLength;
        var dimY = isRotated90 ? targetLength : targetWidth;

        if (targetPosX < 0 || targetPosY < 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Las coordenadas del rack no pueden ser negativas.",
                "ERP:RACK_COORDINATES_OUT_OF_BOUNDS");
        }

        var endX = targetPosX + dimX;
        var endY = targetPosY + dimY;

        if (endX > secWidth)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"El rack '{rack.Code}' excede el ancho horizontal (X) de la sección (requiere {endX:F2}m, disponible: {secWidth:F2}m).",
                "ERP:RACK_EXCEEDS_SECTION_WIDTH");
        }

        if (endY > secLength)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"El rack '{rack.Code}' excede el largo vertical (Y) de la sección (requiere {endY:F2}m, disponible: {secLength:F2}m).",
                "ERP:RACK_EXCEEDS_SECTION_LENGTH");
        }

        // 5. Validación de colisión física con otros racks en el mismo nivel
        var existingRacks = await _unitOfWork.Racks.Entities
            .Include(r => r.RacksCoordinates)
            .Include(r => r.RackCapacity)
            .Where(r => r.SectionId == section.Id
                     && r.LevelNumber == rack.LevelNumber
                     && r.Id != rack.Id
                     && r.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var targetX1 = targetPosX;
        var targetX2 = targetPosX + dimX;
        var targetY1 = targetPosY;
        var targetY2 = targetPosY + dimY;

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

            if ((targetX1 + 0.02m < exX2) && (targetX2 - 0.02m > exX1) &&
                (targetY1 + 0.02m < exY2) && (targetY2 - 0.02m > exY1))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    $"La posición o medidas configuradas colisionan físicamente con el rack existente '{existing.Code}' en el nivel {rack.LevelNumber}.",
                    "ERP:RACK_PHYSICAL_COLLISION");
            }
        }

        // 6. Actualización de datos generales
        if (request.RowNumber.HasValue) rack.RowNumber = request.RowNumber.Value;
        if (request.UsageProfile.HasValue) rack.UsageProfile = request.UsageProfile.Value;
        if (request.Status.HasValue)
        {
            if (request.Status.Value != rack.Status)
                rack.StatusChangedAt = DateTime.UtcNow;
            rack.Status = request.Status.Value;

            if (request.Status.Value != RackStatus.Blocked && request.Status.Value != RackStatus.UnderMaintenance && request.UnavailableReason == null)
            {
                rack.UnavailableReason = null;
            }
        }
        if (request.UnavailableReason != null) rack.UnavailableReason = request.UnavailableReason;

        // 2. Actualización de coordenadas
        if (request.PositionX.HasValue || request.PositionY.HasValue || request.PositionZ.HasValue || request.RotationY.HasValue)
        {
            if (rack.RacksCoordinates is null)
            {
                var newCoord = RackProfile.ToRackCoordinatesEntity(
                    rack.Id,
                    request.PositionX ?? 0m,
                    request.PositionY ?? 0m,
                    request.PositionZ ?? 0m,
                    request.RotationY ?? 0m);
                await _unitOfWork.RackCoordinates.RegisterRackCoordinate(newCoord);
            }
            else
            {
                if (request.PositionX.HasValue) rack.RacksCoordinates.PositionX = request.PositionX.Value;
                if (request.PositionY.HasValue) rack.RacksCoordinates.PositionY = request.PositionY.Value;
                if (request.PositionZ.HasValue) rack.RacksCoordinates.PositionZ = request.PositionZ.Value;
                if (request.RotationY.HasValue) rack.RacksCoordinates.RotationY = request.RotationY.Value;
                await _unitOfWork.RackCoordinates.UpdateAsync(rack.RacksCoordinates);
            }
        }

        // 3. Actualización de medidas y recálculo de capacidad
        if (request.Width.HasValue || request.Length.HasValue || request.Height.HasValue)
        {
            var calc = await capacityCalculator.UpdateRackAsync(
                rack.Id, request.Width, request.Length, request.Height, cancellationToken);

            if (calc.Rack != null)
            {
                if (rack.RackCapacity is null)
                {
                    calc.Rack.RackId = rack.Id;
                    await _unitOfWork.RackCapacities.RegisterRackCapacity(calc.Rack);
                }
                else
                {
                    _mapper.Map(calc.Rack, rack.RackCapacity);
                    await _unitOfWork.RackCapacities.UpdateAsync(rack.RackCapacity);
                }
            }

            await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
            await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);
        }

        await _unitOfWork.Racks.UpdateAsync(rack);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Actualización integral del rack {RackId} completada.", rack.Id);

        return true;
    }
}
