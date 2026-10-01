using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class PatchLotCoordinatesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, AutoMapper.IMapper mapper, ILogger<PatchLotCoordinatesHandler> logger)
    : BaseLotsCapacityHandler<PatchLotCoordinatesCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(PatchLotCoordinatesCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(request.UserId, request.CompanyId,
            request.ModuleCode, request.SectionId, request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (lotCandidate, lotIsValid, lotError) = await GetExistingLotAsync(request.LotId, request.SectionId, cancellationToken);
        if (!lotIsValid) return lotError;

        var lot = lotCandidate!;

        if (lot.LotsCoordinates is null)
            return _errorManager.ThrowBadRequest<bool>(
                "El tramo no tiene coordenadas registradas. Utilice el endpoint de registro.",
                "ERP:LOT_COORDINATES_NOT_FOUND");

        var coordinates = lot.LotsCoordinates;

        if (request.PositionX.HasValue) coordinates.PositionX = request.PositionX.Value;
        if (request.PositionY.HasValue) coordinates.PositionY = request.PositionY.Value;
        if (request.PositionZ.HasValue) coordinates.PositionZ = request.PositionZ.Value;
        if (request.RotationY.HasValue) coordinates.RotationY = request.RotationY.Value;

        if (!ValidateLotPlacement(lot, section!, coordinates.PositionX, coordinates.PositionY))
            return false;

        await _unitOfWork.LotCoordinates.UpdateAsync(coordinates);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅Coordenadas de tramo actualizadas correctamente.");

        return true;
    }
}