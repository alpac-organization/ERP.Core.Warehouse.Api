using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
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

public class LiftStockToMemoryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<LiftStockToMemoryCommand, List<ReassignmentMemoryItemDto>>(unitOfWork, errorManager)
{
    public override async Task<List<ReassignmentMemoryItemDto>> Handle(LiftStockToMemoryCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var userIdStr = request.UserId.ToString();
        var nowNica = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(nowNica);
        var nowTime = TimeOnly.FromDateTime(nowNica);

        var session = await _unitOfWork.ReassignmentSessions.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.DeletedAt == null, cancellationToken);

        if (session is null)
            return _errorManager.ThrowNotFound<List<ReassignmentMemoryItemDto>>(
                "La sesión de reasignamiento no existe.",
                "ERP:REASSIGNMENT_SESSION_NOT_FOUND");

        if (session.Status != ReassignmentSessionStatus.Open)
            return _errorManager.ThrowBadRequest<List<ReassignmentMemoryItemDto>>(
                "La sesión no está abierta; no se pueden levantar polines.",
                "ERP:REASSIGNMENT_SESSION_NOT_OPEN");

        if (session.CurrentOwnerUserId != userIdStr)
            return _errorManager.ThrowForbidden<List<ReassignmentMemoryItemDto>>(
                "Solo el dueño actual de la sesión puede operar sobre ella.",
                "ERP:NOT_SESSION_OWNER");

        var createdItems = new List<ReassignmentMemoryItemDto>();

        foreach (var item in request.Items)
        {
            var placement = await _unitOfWork.StockPlacements.Entities
                .Include(p => p.Stock)
                .Include(p => p.RackPosition)
                .Include(p => p.LotPosition)
                .FirstOrDefaultAsync(p => p.StockId == item.StockId && p.VacatedAtDate == null && p.DeletedAt == null, cancellationToken);

            if (placement is null || placement.Stock is null || placement.Stock.DeletedAt != null)
                return _errorManager.ThrowNotFound<List<ReassignmentMemoryItemDto>>(
                    $"El polín {item.StockId} no existe o no tiene una posición activa en el almacén.",
                    "ERP:STOCK_NOT_PLACED");

            var section = placement.RackPosition?.Rack?.Section ?? placement.LotPosition?.Lot?.Section;

            if (section is null || section.WarehouseId != session.WarehouseId)
                return _errorManager.ThrowBadRequest<List<ReassignmentMemoryItemDto>>(
                    $"El polín {item.StockId} pertenece a un almacén distinto al de la sesión.",
                    "ERP:STOCK_IN_DIFFERENT_WAREHOUSE");

            if (item.TargetRackPositionId.HasValue)
            {
                var targetPosition = await _unitOfWork.RackPositions.Entities
                    .FirstOrDefaultAsync(p => p.Id == item.TargetRackPositionId.Value, cancellationToken);

                if (targetPosition is null)
                    return _errorManager.ThrowNotFound<List<ReassignmentMemoryItemDto>>(
                        $"La posición destino rack {item.TargetRackPositionId} no existe.",
                        "ERP:TARGET_POSITION_NOT_FOUND");

                if (targetPosition.IsOccupied || targetPosition.IsReserved || targetPosition.IsBlocked)
                    return _errorManager.ThrowBadRequest<List<ReassignmentMemoryItemDto>>(
                        $"La posición destino rack {targetPosition.PositionCode} no está disponible.",
                        "ERP:TARGET_POSITION_NOT_AVAILABLE");
            }

            if (item.TargetLotPositionId.HasValue)
            {
                var targetPosition = await _unitOfWork.LotsPositions.Entities
                    .FirstOrDefaultAsync(p => p.Id == item.TargetLotPositionId.Value, cancellationToken);

                if (targetPosition is null)
                    return _errorManager.ThrowNotFound<List<ReassignmentMemoryItemDto>>(
                        $"La posición destino tramo {item.TargetLotPositionId} no existe.",
                        "ERP:TARGET_POSITION_NOT_FOUND");

                if (targetPosition.IsOccupied || targetPosition.IsReserved || targetPosition.IsBlocked)
                    return _errorManager.ThrowBadRequest<List<ReassignmentMemoryItemDto>>(
                        $"La posición destino tramo {targetPosition.PositionCode} no está disponible.",
                        "ERP:TARGET_POSITION_NOT_AVAILABLE");
            }

            var memoryItem = item.ToMemoryItemEntity(request.SessionId, userIdStr, nowDate, nowTime);

            placement.VacatedAtDate = nowDate;
            placement.VacatedAtTime = nowTime;
            placement.VacatedByUserId = userIdStr;
            placement.VacatedByMemoryItemId = memoryItem.Id;

            if (placement.RackPosition is not null)
                placement.RackPosition.IsOccupied = false;

            if (placement.LotPosition is not null)
                placement.LotPosition.IsOccupied = false;

            if (item.TargetRackPositionId.HasValue)
            {
                var targetRack = await _unitOfWork.RackPositions.Entities
                    .FirstAsync(p => p.Id == item.TargetRackPositionId.Value, cancellationToken);
                targetRack.IsReserved = true;
            }

            if (item.TargetLotPositionId.HasValue)
            {
                var targetLot = await _unitOfWork.LotsPositions.Entities
                    .FirstAsync(p => p.Id == item.TargetLotPositionId.Value, cancellationToken);
                targetLot.IsReserved = true;
            }

            await _unitOfWork.ReassignmentMemoryItems.InsertReassignmentMemoryItem(memoryItem);
            createdItems.Add(mapper.Map<ReassignmentMemoryItemDto>(memoryItem));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return createdItems;
    }
}
