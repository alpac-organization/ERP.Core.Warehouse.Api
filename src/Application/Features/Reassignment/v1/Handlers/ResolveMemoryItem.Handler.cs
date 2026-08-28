using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class ResolveMemoryItemHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<ResolveMemoryItemCommand, ReassignmentMemoryItemDto>(unitOfWork, errorManager)
{
    public override async Task<ReassignmentMemoryItemDto> Handle(
        ResolveMemoryItemCommand request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        await ValidateSession(request.SessionId, request.UserId.ToString(), cancellationToken);
        var memoryItem = await ValidateMemoryItem(request.MemoryItemId, request.SessionId, cancellationToken);

        var nowNica = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(nowNica);
        var nowTime = TimeOnly.FromDateTime(nowNica);

        await ConfirmDestination(memoryItem, request.UserId.ToString(), nowDate, nowTime, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<ReassignmentMemoryItemDto>(memoryItem);
    }

    private async Task<ReassignmentSessions> ValidateSession(Guid sessionId, string userIdStr, CancellationToken ct)
    {
        var session = await _unitOfWork.ReassignmentSessions.Entities
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.DeletedAt == null, ct);

        if (session is null)
            return _errorManager.ThrowNotFound<ReassignmentSessions>(
                "La sesión de reasignamiento no existe.",
                "ERP:REASSIGNMENT_SESSION_NOT_FOUND");

        if (session.Status != ReassignmentSessionStatus.Open)
            return _errorManager.ThrowBadRequest<ReassignmentSessions>(
                "La sesión no está abierta; no se puede confirmar el polín.",
                "ERP:REASSIGNMENT_SESSION_NOT_OPEN");

        if (session.CurrentOwnerUserId != userIdStr)
            return _errorManager.ThrowForbidden<ReassignmentSessions>(
                "Solo el dueño actual de la sesión puede operar sobre ella.",
                "ERP:NOT_SESSION_OWNER");

        return session;
    }

    private async Task<ReassignmentMemoryItems> ValidateMemoryItem(Guid memoryItemId, Guid sessionId, CancellationToken ct)
    {
        var memoryItem = await _unitOfWork.ReassignmentMemoryItems.Entities
            .FirstOrDefaultAsync(m => m.Id == memoryItemId && m.DeletedAt == null, ct);

        if (memoryItem is null)
            return _errorManager.ThrowNotFound<ReassignmentMemoryItems>(
                "El polín en aire no existe.",
                "ERP:MEMORY_ITEM_NOT_FOUND");

        if (memoryItem.ReassignmentSessionId != sessionId)
            return _errorManager.ThrowBadRequest<ReassignmentMemoryItems>(
                "El polín en aire no pertenece a esta sesión.",
                "ERP:MEMORY_ITEM_SESSION_MISMATCH");

        if (memoryItem.ResolvedAtDate != null)
            return _errorManager.ThrowBadRequest<ReassignmentMemoryItems>(
                "El polín en aire ya fue confirmado.",
                "ERP:MEMORY_ITEM_ALREADY_RESOLVED");

        return memoryItem;
    }

    private async Task ConfirmDestination(
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
            _errorManager.ThrowBadRequest<ReassignmentMemoryItems>(
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
        var target = await _unitOfWork.RackPositions.Entities
            .FirstOrDefaultAsync(p => p.Id == memoryItem.TargetRackPositionId, ct);

        if (target is null)
        {
            _errorManager.ThrowNotFound<object>(
                $"La posición destino rack {memoryItem.TargetRackPositionId} no existe.",
                "ERP:TARGET_POSITION_NOT_FOUND");
            return;
        }

        if (!target.IsReserved || target.IsOccupied || target.IsBlocked)
        {
            _errorManager.ThrowBadRequest<object>(
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
        var target = await _unitOfWork.LotsPositions.Entities
            .FirstOrDefaultAsync(p => p.Id == memoryItem.TargetLotPositionId, ct);

        if (target is null)
        {
            _errorManager.ThrowNotFound<object>(
                $"La posición destino tramo {memoryItem.TargetLotPositionId} no existe.",
                "ERP:TARGET_POSITION_NOT_FOUND");
            return;
        }

        if (!target.IsReserved || target.IsOccupied || target.IsBlocked)
        {
            _errorManager.ThrowBadRequest<object>(
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

        await _unitOfWork.StockPlacements.InsertStockPlacement(placement);
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
