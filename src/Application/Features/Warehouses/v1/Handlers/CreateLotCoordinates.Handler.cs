using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class CreateLotCoordinatesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager,
    AutoMapper.IMapper mapper, ILogger<CreateLotCoordinatesHandler> logger)
    : BaseLotsCapacityHandler<CreateLotCoordinatesCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(CreateLotCoordinatesCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(request.UserId, request.CompanyId,
            request.ModuleCode, request.SectionId, request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (lotCandidate, lotIsValid, lotError) = await GetExistingLotAsync(request.LotId, request.SectionId, cancellationToken);
        if (!lotIsValid) return lotError;

        var lot = lotCandidate!;

        if (lot.LotsCoordinates is not null)
            return _errorManager.ThrowBadRequest<bool>(
                "El tramo ya tiene coordenadas registradas. Utilice el endpoint de actualización.",
                "ERP:LOT_COORDINATES_ALREADY_EXIST");

        var positionX = request.PositionX ?? 0;
        var positionY = request.PositionY ?? 0;

        if (!ValidateLotPlacement(lot, section!, positionX, positionY))
            return false;

        var entity = request.ToLotCoordinatesEntity(lot.Id);

        await _unitOfWork.LotCoordinates.RegisterLotCoordinate(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅Coordenadas de tramo registradas correctamente.");

        return true;
    }
}