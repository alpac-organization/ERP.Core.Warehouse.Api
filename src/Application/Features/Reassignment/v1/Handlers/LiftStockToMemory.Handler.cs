using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class LiftStockToMemoryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<LiftStockToMemoryCommand, ReassignmentMemoryItemDto>(unitOfWork, errorManager)
{
    public override async Task<ReassignmentMemoryItemDto> Handle(LiftStockToMemoryCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var userIdStr = request.UserId.ToString();

        var session = await _unitOfWork.ReassignmentSessions.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.DeletedAt == null, cancellationToken);

        if (session is null)
            return _errorManager.ThrowNotFound<ReassignmentMemoryItemDto>(
                "La sesión de reasignamiento no existe.",
                "ERP:REASSIGNMENT_SESSION_NOT_FOUND");

        if (session.Status != ReassignmentSessionStatus.Open)
            return _errorManager.ThrowBadRequest<ReassignmentMemoryItemDto>(
                "La sesión no está abierta; no se pueden levantar polines.",
                "ERP:REASSIGNMENT_SESSION_NOT_OPEN");

        if (session.CurrentOwnerUserId != userIdStr)
            return _errorManager.ThrowForbidden<ReassignmentMemoryItemDto>(
                "Solo el dueño actual de la sesión puede operar sobre ella.",
                "ERP:NOT_SESSION_OWNER");

        var activePlacements = await _unitOfWork.StockPlacements.Entities
            .Include(p => p.Stock)
            .Include(p => p.RackPosition).ThenInclude(rp => rp!.Rack).ThenInclude(r => r.Section!)
            .Include(p => p.LotPosition).ThenInclude(lp => lp!.Lot).ThenInclude(l => l!.Section)
            .Where(p => p.StockId == request.StockId && p.VacatedAtDate == null && p.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (activePlacements.Count == 0)
            return _errorManager.ThrowNotFound<ReassignmentMemoryItemDto>(
                "El polín indicado no existe o no tiene una posición activa en el almacén.",
                "ERP:STOCK_NOT_PLACED");

        var placement = activePlacements[0];

        if (placement.Stock is null || placement.Stock.DeletedAt != null)
            return _errorManager.ThrowNotFound<ReassignmentMemoryItemDto>(
                "El polín indicado no existe o no tiene una posición activa en el almacén.",
                "ERP:STOCK_NOT_PLACED");

        if (activePlacements.Count > 1)
            return _errorManager.ThrowInternalError<ReassignmentMemoryItemDto>(
                "El polín tiene más de una posición activa; es una inconsistencia que debe auditarse.",
                "ERP:MULTIPLE_ACTIVE_PLACEMENTS");

        var section = placement.RackPosition?.Rack?.Section ?? placement.LotPosition?.Lot?.Section;

        if (section is null || section.WarehouseId != session.WarehouseId)
            return _errorManager.ThrowBadRequest<ReassignmentMemoryItemDto>(
                "El polín pertenece a un almacén distinto al de la sesión.",
                "ERP:STOCK_IN_DIFFERENT_WAREHOUSE");

        var memoryItem = request.ToMemoryItemEntity(userIdStr);

        foreach (var activePlacement in activePlacements)
            VacatePlacement(activePlacement, memoryItem, userIdStr);

        await _unitOfWork.ReassignmentMemoryItems.InsertReassignmentMemoryItem(memoryItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<ReassignmentMemoryItemDto>(memoryItem);
    }

    private static void VacatePlacement(StockPlacements placement, ReassignmentMemoryItems memoryItem, string userId)
    {
        placement.VacatedAtDate = memoryItem.LiftedAtDate;
        placement.VacatedAtTime = memoryItem.LiftedAtTime;
        placement.VacatedByUserId = userId;
        placement.VacatedByMemoryItemId = memoryItem.Id;

        if (placement.RackPosition is not null)
            placement.RackPosition.IsOccupied = false;

        if (placement.LotPosition is not null)
            placement.LotPosition.IsOccupied = false;
    }
}
