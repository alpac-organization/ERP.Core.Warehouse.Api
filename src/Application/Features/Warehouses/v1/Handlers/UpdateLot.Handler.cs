using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class UpdateLotHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, AutoMapper.IMapper mapper,
    ILotCapacityCalculator capacityCalculator, ICodeGenerator codeGenerator, ILogger<UpdateLotHandler> logger)
    : BaseLotsCapacityHandler<UpdateLotCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(UpdateLotCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando proceso de actualización de tramo.");

        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId, request.CompanyId, request.ModuleCode, request.SectionId,
            request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        var (lotCandidate, lotIsValid, lotError) = await GetExistingLotAsync(
            request.LotId, request.SectionId, cancellationToken);
        if (!lotIsValid) return lotError;

        var lot = lotCandidate!;

        if (request.AllowsStacking.HasValue) lot.AllowsStacking = request.AllowsStacking.Value;
        if (request.Status.HasValue)
        {
            if (request.Status.Value != lot.Status)
                lot.StatusChangedAt = DateTime.UtcNow;
            lot.Status = request.Status.Value;
        }
        if (request.UnavailableReason != null) lot.UnavailableReason = request.UnavailableReason;

        await ReconcilePositionsAsync(lot, request, cancellationToken);

        var calc = await capacityCalculator.UpdateLotAsync(
            lot.Id, request.WidthMetres, request.LengthMetres, cancellationToken);

        var calculatedLot = calc.Lot!;

        if (lot.LotsCapacity is null)
        {
            calculatedLot.LotsId = lot.Id;
            await _unitOfWork.LotsCapacities.RegisterLotsCapacity(calculatedLot);
        }
        else
        {
            _mapper.Map(calculatedLot, lot.LotsCapacity);
            await _unitOfWork.LotsCapacities.UpdateAsync(lot.LotsCapacity);
        }

        await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Lots.UpdateAsync(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅Tramo actualizado correctamente.");

        return true;
    }

    private async Task ReconcilePositionsAsync(
        Lots lot,
        UpdateLotCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.NominalRows.HasValue && !request.NominalColumns.HasValue)
            return;

        var targetRows = request.NominalRows ?? lot.NominalRows;
        var targetColumns = request.NominalColumns ?? lot.NominalColumns;

        if (targetRows is null || targetColumns is null)
        {
            _errorManager.ThrowBadRequest<bool>(
                "El tramo no tiene definidas filas y columnas para actualizar su matriz.",
                "ERP:LOT_MATRIX_REQUIRED");
        }

        var rows = targetRows!.Value;
        var columns = targetColumns!.Value;

        var activePositions = lot.Positions?
            .Where(p => p.DeletedAt == null)
            .ToList() ?? [];

        var positionsToRemove = activePositions
            .Where(p => p.Row > rows || p.Column > columns)
            .ToList();

        await ValidatePositionsAvailableForRemovalAsync(positionsToRemove, cancellationToken);

        if (positionsToRemove.Count > 0)
            await _unitOfWork.LotsPositions.RemoveRangeAsync(positionsToRemove);

        var positionMap = activePositions.ToDictionary(p => (p.Row, p.Column));

        for (int row = 1; row <= rows; row++)
        {
            for (int column = 1; column <= columns; column++)
            {
                if (positionMap.ContainsKey((row, column)))
                    continue;

                var newPosition = LotsProfile.ToLotsPositionEntity(
                    lot.Id,
                    codeGenerator.GeneratePositionCode(lot.Code, row, column),
                    row,
                    column);

                await _unitOfWork.LotsPositions.RegisterLotPosition(newPosition);
            }
        }

        lot.NominalRows = rows;
        lot.NominalColumns = columns;
    }

    private async Task ValidatePositionsAvailableForRemovalAsync(
        List<LotsPositions> positionsToRemove,
        CancellationToken cancellationToken)
    {
        if (positionsToRemove.Count == 0)
            return;

        var positionIds = positionsToRemove.Select(p => p.Id!.Value).ToList();

        var hasReferences = await _unitOfWork.StockPlacements.Entities
            .AnyAsync(
                s => s.LotPositionId != null
                    && positionIds.Contains(s.LotPositionId.Value),
                cancellationToken);

        if (!hasReferences)
        {
            hasReferences = await _unitOfWork.WarehouseAssignments.Entities
                .AnyAsync(
                    a => a.LotsPositionsId != null
                        && positionIds.Contains(a.LotsPositionsId.Value),
                    cancellationToken);
        }

        if (hasReferences)
        {
            _errorManager.ThrowBadRequest<bool>(
                "No se pueden eliminar posiciones que tengan stock o asignaciones registradas.",
                "ERP:LOT_POSITION_HAS_REFERENCES");
        }
    }
}