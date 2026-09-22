using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class DeleteLotHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, AutoMapper.IMapper mapper,
    ILotCapacityCalculator capacityCalculator)
    : BaseLotsCapacityHandler<DeleteLotCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId, request.CompanyId, request.ModuleCode, request.SectionId,
            request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (lotCandidate, lotIsValid, lotError) = await GetExistingLotAsync(
            request.LotId, request.SectionId, cancellationToken);
        if (!lotIsValid) return lotError;

        var lot = lotCandidate!;

        var activePositions = lot.Positions?
            .Where(p => p.DeletedAt == null)
            .ToList() ?? [];

        await ValidateNoActiveStockAsync(activePositions, cancellationToken);

        var calc = await capacityCalculator.DeleteLotAsync(lot.Id, cancellationToken);

        foreach (var position in activePositions)
        {
            position.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.LotsPositions.UpdateAsync(position);
        }

        lot.DeletedAt = DateTime.UtcNow;

        lot.LotsCapacity?.DeletedAt = DateTime.UtcNow;

        await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Lots.UpdateAsync(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidateNoActiveStockAsync(
        List<LotsPositions> positions,
        CancellationToken cancellationToken)
    {
        if (positions.Count == 0)
            return;

        var positionIds = positions.Select(p => p.Id).ToList();

        var hasActiveStock = await _unitOfWork.StockPlacements.Entities
            .AnyAsync(
                s => s.LotPositionId != null
                    && positionIds.Contains(s.LotPositionId.Value)
                    && s.VacatedAtDate == null
                    && s.DeletedAt == null,
                cancellationToken);

        if (hasActiveStock)
            _errorManager.ThrowBadRequest<bool>(
                "No se puede eliminar el tramo porque tiene stock activo en sus posiciones.",
                "ERP:LOT_HAS_ACTIVE_STOCK");
    }
}
