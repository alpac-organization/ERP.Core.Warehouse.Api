using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;

using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{

    public class RegisterCoordinatesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ILogger<RegisterCoordinatesHandler> logger) : BaseValidatorHandler<RegisterCoordinatesCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(RegisterCoordinatesCommand request, CancellationToken cancellationToken)
        {

            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType != RoleType.Administrator)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            await ValidateWarehouseAndSection(request, cancellationToken);

            switch (request.TargetType)
            {
                case CoordinateTargetType.LotsPositions:
                {
                    var parentLot = await _unitOfWork.Lots.Entities
                        .Where(lot => lot.Id == request.LotId)
                        .Where(lot => lot.SectionId == request.SectionId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (parentLot is null)
                    {
                        return _errorManager.ThrowBadRequest<Unit>("El tramo no existe para registro de coordenadas perteneciente a este tramo", "ERP:01");
                    }

                    await RegisterLotsPositionsCoordinatesAsync(request.LotsPositionsInformation, parentLot.Id, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Unit.Value;
                }

                case CoordinateTargetType.RackPositions:
                {
                    var parentRack = await _unitOfWork.Racks.Entities
                        .Where(rack => rack.Id == request.RackId)
                        .Where(rack => rack.SectionId == request.SectionId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (parentRack is null)
                    {
                        return _errorManager.ThrowBadRequest<Unit>("El rack no existe para registro de coordenadas perteneciente a este tramo", "ERP:01");
                    }

                    await RegisterRackPositionsCoordinatesAsync(request.RackPositionsInformation, parentRack.Id, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Unit.Value;   
                }
                default:
                {
                    return _errorManager.ThrowBadRequest<Unit>("El target del registro no es valido", "ERP:INVALID_TARGET");   
                }
            }
        }

        private async Task<Unit> ValidateWarehouseAndSection(RegisterCoordinatesCommand command, CancellationToken cancellationToken)
        {
            var warehouse = await _unitOfWork.Warehouses.Entities
                .Where(warehouse => warehouse.IsActive)
                .Where(warehouse => warehouse.Id == command.WarehouseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (warehouse is null)
            {
                return _errorManager.ThrowBadRequest<Unit>("La bodega a la que intentas realizar el registro no existe", "ERP:NOT_FOUND");
            }

            var section = await _unitOfWork.Sections.Entities
                .Where(section => section.IsActive)
                .Where(section => section.Id == command.SectionId)
                .Where(section => section.WarehouseId == warehouse.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (section is null)
            {
                return _errorManager.ThrowBadRequest<Unit>("La seccion selccionada no pertene o exite para esta bodega", "ERP:NOT_FOUND");
            }

            return Unit.Value;
        }

        private async Task<bool> RegisterLotsPositionsCoordinatesAsync(List<LotsPositionsInformation> positionsInfo, Guid LotId, CancellationToken cancellationToken)
        {
            foreach (var positionInfo in positionsInfo)
            {
                var lotPosition = await _unitOfWork.LotsPositions.Entities
                    .Where(lp => lp.LotId == LotId)
                    .Where(lp => lp.Id == positionInfo.LotPositionId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lotPosition is null)
                {
                    logger.LogWarning("⚠️ Posición de tramo {LotPositionId} no encontrada, omitiendo.", positionInfo.LotPositionId);
                    continue;
                }

                var lotPositionCoordinates = await _unitOfWork.LotsPositionsCoordinates.Entities
                    .Where(lotPositionCoordinate => lotPositionCoordinate.LotPositionId == lotPosition.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lotPositionCoordinates is not null)
                {
                    lotPositionCoordinates.PositionX = positionInfo?.PositionX ?? lotPositionCoordinates.PositionX;
                    lotPositionCoordinates.PositionY = positionInfo?.PositionX ?? lotPositionCoordinates.PositionY;
                    lotPositionCoordinates.PositionZ = positionInfo?.PositionX ?? lotPositionCoordinates.PositionZ;
                    lotPositionCoordinates.RotationY = positionInfo?.RotationY ?? lotPositionCoordinates.RotationY;

                    await _unitOfWork.LotsPositionsCoordinates.UpdateAsync(lotPositionCoordinates);
                }
                else
                {                    
                    var coordinate = new LotsPositionsCoordinates
                    {
                        LotPositionId = positionInfo.LotPositionId,
                        PositionX = positionInfo.PositionX,
                        PositionY = positionInfo.PositionY,
                        PositionZ = positionInfo.PositionZ,
                        RotationY = positionInfo.RotationY
                    };

                    await _unitOfWork.LotsPositionsCoordinates.RegisterLotPositionCoordinate(coordinate);
                }
            }

            return true;
        }

        private async Task<bool> RegisterRackPositionsCoordinatesAsync(List<RackPositionsInformation> positionsInfo, Guid rackId, CancellationToken cancellationToken)
        {

            foreach (var rackInfo in positionsInfo)
            {

                var rackPosition = await _unitOfWork.RackPositions.Entities
                    .Include(rk => rk.Rack)
                    .Where(rk => rk.RackId == rackId)
                    .Where(rk => rk.Id == rackInfo.RackPositionId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (rackPosition is null)
                {
                    logger.LogWarning("⚠️ Posición de rack {RackPositionId} no encontrada, omitiendo.", rackInfo.RackPositionId);
                    continue;
                }

                var rackPositionCoordinates = await _unitOfWork.RacksPositionsCoordinates.Entities
                    .Where(rackp => rackp.RackPositionId == rackPosition.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (rackPositionCoordinates is not null)
                {
                    rackPositionCoordinates.PositionX = rackInfo?.PositionX ?? rackPositionCoordinates.PositionX;
                    rackPositionCoordinates.PositionY = rackInfo?.PositionX ?? rackPositionCoordinates.PositionY;
                    rackPositionCoordinates.PositionZ = rackInfo?.PositionX ?? rackPositionCoordinates.PositionZ;
                    rackPositionCoordinates.RotationY = rackInfo?.RotationY ?? rackPositionCoordinates.RotationY;

                    await _unitOfWork.RacksPositionsCoordinates.UpdateAsync(rackPositionCoordinates);
                }
                else
                {
                    var coordinate = new RacksPositionsCoordinates
                    {
                        RackPositionId = rackInfo.RackPositionId,
                        PositionX = rackInfo.PositionX,
                        PositionY = rackInfo.PositionY,
                        PositionZ = rackInfo.PositionZ,
                        RotationY = rackInfo.RotationY
                    };
                 
                    await _unitOfWork.RacksPositionsCoordinates.RegisterRackPositionCoordinate(coordinate);
                }
            }

            return true;
        }
    }
    
}

