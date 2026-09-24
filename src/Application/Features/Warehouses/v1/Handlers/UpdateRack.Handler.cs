using AutoMapper;
using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
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

        // 1. Actualización de datos generales
        if (request.RowNumber.HasValue) rack.RowNumber = request.RowNumber.Value;
        if (request.UsageProfile.HasValue) rack.UsageProfile = request.UsageProfile.Value;
        if (request.Status.HasValue)
        {
            if (request.Status.Value != rack.Status)
                rack.StatusChangedAt = DateTime.UtcNow;
            rack.Status = request.Status.Value;
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
