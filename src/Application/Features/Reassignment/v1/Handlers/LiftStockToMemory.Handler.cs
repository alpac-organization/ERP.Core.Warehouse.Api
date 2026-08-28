using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class LiftStockToMemoryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper, SessionAccessValidator sessionValidator)
    : BaseValidatorHandler<LiftStockToMemoryCommand, List<ReassignmentMemoryItemDto>>(unitOfWork, errorManager)
{
    public override async Task<List<ReassignmentMemoryItemDto>> Handle(LiftStockToMemoryCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var session = await sessionValidator.ValidateSession(request.SessionId, request.UserId.ToString(), cancellationToken);

        var nowNica = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(nowNica);
        var nowTime = TimeOnly.FromDateTime(nowNica);

        var createdItems = new List<ReassignmentMemoryItemDto>();

        foreach (var item in request.Items)
            createdItems.Add(await ProcessLiftItem(item, session, request.UserId.ToString(), nowDate, nowTime, cancellationToken));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return createdItems;
    }

    private async Task<ReassignmentMemoryItemDto> ProcessLiftItem(LiftStockItemDto item, ReassignmentSessions session, string userIdStr, DateOnly nowDate, TimeOnly nowTime, CancellationToken ct)
    {
        var placement = await ValidatePlacement(item.StockId, session.WarehouseId, ct);

        await ValidateAndReserveTarget(item, ct);

        var memoryItem = item.ToMemoryItemEntity(session.Id, userIdStr, nowDate, nowTime);

        VacatePlacement(placement, memoryItem, userIdStr);

        await _unitOfWork.ReassignmentMemoryItems.InsertReassignmentMemoryItem(memoryItem);

        return mapper.Map<ReassignmentMemoryItemDto>(memoryItem);
    }

    private async Task<StockPlacements> ValidatePlacement(Guid stockId, Guid warehouseId, CancellationToken ct)
    {
        var placement = await _unitOfWork.StockPlacements.Entities
            .Include(p => p.Stock)
            .Include(p => p.RackPosition)
            .Include(p => p.LotPosition)
            .FirstOrDefaultAsync(p => p.StockId == stockId && p.VacatedAtDate == null && p.DeletedAt == null, ct);

        if (placement is null || placement.Stock is null || placement.Stock.DeletedAt != null)
            return _errorManager.ThrowNotFound<StockPlacements>(
                $"El polín {stockId} no existe o no tiene una posición activa en el almacén.",
                "ERP:STOCK_NOT_PLACED");

        var section = placement.RackPosition?.Rack?.Section ?? placement.LotPosition?.Lot?.Section;

        if (section is null || section.WarehouseId != warehouseId)
            return _errorManager.ThrowBadRequest<StockPlacements>(
                $"El polín {stockId} pertenece a un almacén distinto al de la sesión.",
                "ERP:STOCK_IN_DIFFERENT_WAREHOUSE");

        return placement;
    }

    private async Task ValidateAndReserveTarget(LiftStockItemDto item, CancellationToken ct)
    {
        if (item.TargetRackPositionId.HasValue)
        {
            var target = await _unitOfWork.RackPositions.Entities
                .FirstOrDefaultAsync(p => p.Id == item.TargetRackPositionId.Value, ct);

            if (target is null)
            {
                _errorManager.ThrowNotFound<object>(
                    $"La posición destino rack {item.TargetRackPositionId} no existe.",
                    "ERP:TARGET_POSITION_NOT_FOUND");
                return;
            }

            if (target.IsOccupied || target.IsReserved || target.IsBlocked)
            {
                _errorManager.ThrowBadRequest<object>(
                    $"La posición destino rack {target.PositionCode} no está disponible.",
                    "ERP:TARGET_POSITION_NOT_AVAILABLE");
                return;
            }

            target.IsReserved = true;
        }

        if (item.TargetLotPositionId.HasValue)
        {
            var target = await _unitOfWork.LotsPositions.Entities
                .FirstOrDefaultAsync(p => p.Id == item.TargetLotPositionId.Value, ct);

            if (target is null)
            {
                _errorManager.ThrowNotFound<object>(
                    $"La posición destino tramo {item.TargetLotPositionId} no existe.",
                    "ERP:TARGET_POSITION_NOT_FOUND");
                return;
            }

            if (target.IsOccupied || target.IsReserved || target.IsBlocked)
            {
                _errorManager.ThrowBadRequest<object>(
                    $"La posición destino tramo {target.PositionCode} no está disponible.",
                    "ERP:TARGET_POSITION_NOT_AVAILABLE");
                return;
            }

            target.IsReserved = true;
        }
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
