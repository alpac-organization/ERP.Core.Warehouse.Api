using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class ReassignmentProfile : Profile
{
    public ReassignmentProfile()
    {
        #region Issue 1 - Abrir sesión
        CreateMap<ReassignmentSessions, ReassignmentSessionDto>()
            .ForMember(d => d.SessionId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.OpenedAt, o => o.MapFrom(s => s.OpenedAtDate.ToDateTime(s.OpenedAtTime)))
            .ForMember(d => d.ClosedAt, o => o.MapFrom(s =>
                s.ClosedAtDate.HasValue && s.ClosedAtTime.HasValue
                    ? s.ClosedAtDate.Value.ToDateTime(s.ClosedAtTime.Value)
                    : (DateTime?)null));
        #endregion

        #region Issue 2 - Levantar polines
        CreateMap<ReassignmentMemoryItems, ReassignmentMemoryItemDto>()
            .ForMember(d => d.MemoryItemId, o => o.MapFrom(s => s.Id));
        #endregion
    }
}

#region Issue 1 - Abrir sesión
public static class OpenReassignmentSessionMapper
{
    public static ReassignmentSessions ToSessionEntity(this OpenReassignmentSessionCommand command, string userId)
    {
        var sessionId = Guid.NewGuid();
        var nowNica = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(nowNica);
        var nowTime = TimeOnly.FromDateTime(nowNica);

        return new ReassignmentSessions
        {
            Id = sessionId,
            WarehouseId = command.WarehouseId,
            Status = ReassignmentSessionStatus.Open,
            CurrentOwnerUserId = userId,
            OpenedAtDate = nowDate,
            OpenedAtTime = nowTime,
            OpenedByUserId = userId,
            OwnershipLog =
            [
                new ReassignmentSessionOwnershipLog
                {
                    Id = Guid.NewGuid(),
                    ReassignmentSessionId = sessionId,
                    UserId = userId,
                    StartedAtDate = nowDate,
                    StartedAtTime = nowTime
                }
            ]
        };
    }
}
#endregion

#region Issue 2 - Levantar polines
public static class LiftStockToMemoryMapper
{
    public static ReassignmentMemoryItems ToMemoryItemEntity(this LiftStockItemDto item, Guid sessionId, string userId, DateOnly nowDate, TimeOnly nowTime)
    {
        return new ReassignmentMemoryItems
        {
            Id = Guid.NewGuid(),
            ReassignmentSessionId = sessionId,
            StockId = item.StockId,
            TargetRackPositionId = item.TargetRackPositionId,
            TargetLotPositionId = item.TargetLotPositionId,
            LiftedAtDate = nowDate,
            LiftedAtTime = nowTime,
            LiftedByUserId = userId
        };
    }
}
#endregion

#region Issue 3 - Posiciones disponibles
public static class GetAvailablePositionsMapper
{
    public static AvailablePositionDto ToAvailablePositionDto(
        this RackPositions position,
        Guid? stockId,
        Guid? reservedBySessionId)
    {
        return new AvailablePositionDto
        {
            PositionId = position.Id,
            PositionCode = position.PositionCode,
            Type = "Rack",
            Status = ResolveStatus(position.IsOccupied, position.IsReserved, position.IsBlocked),
            SectionId = position.Rack.SectionId,
            SectionCode = position.Rack.Section.Code,
            RackId = position.RackId,
            RackCode = position.Rack.Code,
            PositionNumber = position.PositionNumber,
            StockId = stockId,
            ReservedBySessionId = reservedBySessionId
        };
    }

    public static AvailablePositionDto ToAvailablePositionDto(
        this LotsPositions position,
        Guid? stockId,
        Guid? reservedBySessionId)
    {
        return new AvailablePositionDto
        {
            PositionId = position.Id,
            PositionCode = position.PositionCode,
            Type = "Lot",
            Status = ResolveStatus(position.IsOccupied, position.IsReserved, position.IsBlocked),
            SectionId = position.Lot.SectionId,
            SectionCode = position.Lot.Section.Code,
            LotId = position.LotId,
            LotCode = position.Lot.Code,
            RowNumber = position.RowNumber,
            ColumnNumber = position.ColumnNumber,
            StockId = stockId,
            ReservedBySessionId = reservedBySessionId
        };
    }

    public static string ResolveStatus(bool isOccupied, bool isReserved, bool isBlocked)
    {
        if (isOccupied) return "Occupied";
        if (isReserved) return "Reserved";
        if (isBlocked) return "Blocked";
        return "Free";
    }
}
#endregion

#region Issue 4 - Confirmar polin en aire
public static class ResolveMemoryItemMapper
{
    public static StockPlacements ToDestinationPlacementEntity(
        this ReassignmentMemoryItems memoryItem,
        Guid? rackPositionId,
        Guid? lotPositionId,
        string userId,
        DateOnly nowDate,
        TimeOnly nowTime)
    {
        return new StockPlacements
        {
            Id = Guid.NewGuid(),
            StockId = memoryItem.StockId,
            RackPositionId = rackPositionId,
            LotPositionId = lotPositionId,
            PlacedAtDate = nowDate,
            PlacedAtTime = nowTime,
            PlacedByUserId = userId,
            PlacedByMemoryItemId = memoryItem.Id
        };
    }

    public static StockMovementEvents ToStockMovementEventEntity(
        this ReassignmentMemoryItems memoryItem,
        Guid sessionId,
        string userId,
        DateOnly nowDate,
        TimeOnly nowTime)
    {
        return new StockMovementEvents
        {
            Id = Guid.NewGuid(),
            ReassignmentSessionId = sessionId,
            ReassignmentMemoryItemId = memoryItem.Id,
            StockId = memoryItem.StockId,
            ConfirmedAtDate = nowDate,
            ConfirmedAtTime = nowTime,
            ConfirmedByUserId = userId
        };
    }
}
#endregion

#region Issue 5b - Transferir sesión
public static class TransferSessionMapper
{
    public static ReassignmentSessionOwnershipLog ToOwnershipLogEntity(
        this Guid sessionId,
        string newOwnerUserId,
        DateOnly nowDate,
        TimeOnly nowTime)
    {
        return new ReassignmentSessionOwnershipLog
        {
            Id = Guid.NewGuid(),
            ReassignmentSessionId = sessionId,
            UserId = newOwnerUserId,
            StartedAtDate = nowDate,
            StartedAtTime = nowTime
        };
    }
}
#endregion
