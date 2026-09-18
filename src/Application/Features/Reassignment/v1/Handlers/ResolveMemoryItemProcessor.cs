using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Domain.Entities.Warehouse;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class ResolveMemoryItemProcessor(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    SessionAccessValidator sessionValidator)
{
    public Task ValidateSession(Guid sessionId, string userIdStr, CancellationToken ct)
        => sessionValidator.ValidateSession(sessionId, userIdStr, ct);

    public async Task<ReassignmentMemoryItems> ValidateMemoryItem(Guid memoryItemId, Guid sessionId, CancellationToken ct)
    {
        var memoryItem = await unitOfWork.ReassignmentMemoryItems.Entities
            .FirstOrDefaultAsync(m => m.Id == memoryItemId && m.DeletedAt == null, ct);

        if (memoryItem is null)
            return errorManager.ThrowNotFound<ReassignmentMemoryItems>(
                "El polín en aire no existe.",
                "ERP:MEMORY_ITEM_NOT_FOUND");

        if (memoryItem.ReassignmentSessionId != sessionId)
            return errorManager.ThrowBadRequest<ReassignmentMemoryItems>(
                "El polín en aire no pertenece a esta sesión.",
                "ERP:MEMORY_ITEM_SESSION_MISMATCH");

        if (memoryItem.ResolvedAtDate != null)
            return errorManager.ThrowBadRequest<ReassignmentMemoryItems>(
                "El polín en aire ya fue confirmado.",
                "ERP:MEMORY_ITEM_ALREADY_RESOLVED");

        return memoryItem;
    }

    public async Task ConfirmDestination(
        ReassignmentMemoryItems memoryItem,
        string userIdStr,
        DateOnly nowDate,
        TimeOnly nowTime,
        CancellationToken ct)
    {
        if (memoryItem.TargetRackPositionId.HasValue)
            await ConfirmRackDestination(memoryItem, userIdStr, nowDate, nowTime, ct);
        else if (memoryItem.TargetLotPositionId.HasValue)
            await ConfirmLotDestination(memoryItem, userIdStr, nowDate, nowTime, ct);
        else
            errorManager.ThrowBadRequest<ReassignmentMemoryItems>(
                "El polín en aire no tiene posición destino.",
                "ERP:MEMORY_ITEM_NO_TARGET");
    }

    private async Task ConfirmRackDestination(
        ReassignmentMemoryItems memoryItem,
        string userIdStr,
        DateOnly nowDate,
        TimeOnly nowTime,
        CancellationToken ct)
    {
        var target = await unitOfWork.RackPositions.Entities
            .FirstOrDefaultAsync(p => p.Id == memoryItem.TargetRackPositionId, ct);

        if (target is null)
        {
            errorManager.ThrowNotFound<object>(
                $"La posición destino rack {memoryItem.TargetRackPositionId} no existe.",
                "ERP:TARGET_POSITION_NOT_FOUND");
            return;
        }

        if (!target.IsReserved || target.IsOccupied || target.IsBlocked)
        {
            errorManager.ThrowBadRequest<object>(
                $"La posición destino rack {target.PositionCode} no está reservada para este polín.",
                "ERP:TARGET_POSITION_NOT_AVAILABLE");
            return;
        }

        target.IsReserved = false;
        target.IsOccupied = true;

        await InsertDestinationPlacement(memoryItem, target.Id, null, userIdStr, nowDate, nowTime, ct);

        ResolveMemoryItem(memoryItem, userIdStr, nowDate, nowTime);
    }

    private async Task ConfirmLotDestination(
        ReassignmentMemoryItems memoryItem,
        string userIdStr,
        DateOnly nowDate,
        TimeOnly nowTime,
        CancellationToken ct)
    {
        var target = await unitOfWork.LotsPositions.Entities
            .FirstOrDefaultAsync(p => p.Id == memoryItem.TargetLotPositionId, ct);

        if (target is null)
        {
            errorManager.ThrowNotFound<object>(
                $"La posición destino tramo {memoryItem.TargetLotPositionId} no existe.",
                "ERP:TARGET_POSITION_NOT_FOUND");
            return;
        }

        if (!target.IsReserved || target.IsOccupied || target.IsBlocked)
        {
            errorManager.ThrowBadRequest<object>(
                $"La posición destino tramo {target.PositionCode} no está reservada para este polín.",
                "ERP:TARGET_POSITION_NOT_AVAILABLE");
            return;
        }

        target.IsReserved = false;
        target.IsOccupied = true;

        await InsertDestinationPlacement(memoryItem, null, target.Id, userIdStr, nowDate, nowTime, ct);

        ResolveMemoryItem(memoryItem, userIdStr, nowDate, nowTime);
    }

    private async Task InsertDestinationPlacement(
        ReassignmentMemoryItems memoryItem,
        Guid? rackPositionId,
        Guid? lotPositionId,
        string userIdStr,
        DateOnly nowDate,
        TimeOnly nowTime,
        CancellationToken ct)
    {
        var placement = memoryItem.ToDestinationPlacementEntity(
            rackPositionId, lotPositionId, userIdStr, nowDate, nowTime);

        await unitOfWork.StockPlacements.InsertStockPlacement(placement);

        var movementEvent = memoryItem.ToStockMovementEventEntity(
            memoryItem.ReassignmentSessionId, userIdStr, nowDate, nowTime);

        await unitOfWork.StockMovementEvents.InsertStockMovementEvent(movementEvent);
    }

    private static void ResolveMemoryItem(
        ReassignmentMemoryItems memoryItem,
        string userIdStr,
        DateOnly nowDate,
        TimeOnly nowTime)
    {
        memoryItem.ResolvedAtDate = nowDate;
        memoryItem.ResolvedAtTime = nowTime;
        memoryItem.ResolvedByUserId = userIdStr;
    }
}
