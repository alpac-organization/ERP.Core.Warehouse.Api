using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class DeleteRackHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    AutoMapper.IMapper mapper,
    IRackCapacityCalculator capacityCalculator)
    : BaseRacksCapacityHandler<DeleteRackCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(DeleteRackCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId, request.CompanyId, request.ModuleCode, request.SectionId,
            request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (rackCandidate, rackIsValid, rackError) = await GetExistingRackAsync(
            request.RackId, request.SectionId, cancellationToken);
        if (!rackIsValid) return rackError;

        var rack = rackCandidate!;

        var activePositions = rack.Positions?
            .Where(p => p.DeletedAt == null)
            .ToList() ?? [];

        await ValidateNoActiveStockAsync(activePositions, cancellationToken);

        var calc = await capacityCalculator.DeleteRackAsync(rack.Id, cancellationToken);

        foreach (var position in activePositions)
        {
            position.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.RackPositions.UpdateAsync(position);
        }

        rack.DeletedAt = DateTime.UtcNow;

        if (rack.RackCapacity != null)
            rack.RackCapacity.DeletedAt = DateTime.UtcNow;

        if (rack.RacksCoordinates != null)
            rack.RacksCoordinates.DeletedAt = DateTime.UtcNow;

        await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Racks.UpdateAsync(rack);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidateNoActiveStockAsync(
        List<RackPositions> positions,
        CancellationToken cancellationToken)
    {
        if (positions.Count == 0)
            return;

        var positionIds = positions.Select(p => p.Id).ToList();

        var hasActiveStock = await _unitOfWork.StockPlacements.Entities
            .AnyAsync(
                s => s.RackPositionId != null
                    && positionIds.Contains(s.RackPositionId.Value)
                    && s.VacatedAtDate == null
                    && s.DeletedAt == null,
                cancellationToken);

        if (hasActiveStock)
            _errorManager.ThrowBadRequest<bool>(
                "No se puede eliminar el rack porque tiene stock activo asignado en sus posiciones.",
                "ERP:RACK_HAS_ACTIVE_STOCK");
    }
}
