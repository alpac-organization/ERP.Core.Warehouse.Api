using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class ReassignmentProfile : Profile
{
    public ReassignmentProfile()
    {
        CreateMap<ReassignmentSessions, ReassignmentSessionDto>()
            .ForMember(d => d.SessionId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.OpenedAt, o => o.MapFrom(s => s.OpenedAtDate.ToDateTime(s.OpenedAtTime)))
            .ForMember(d => d.ClosedAt, o => o.MapFrom(s =>
                s.ClosedAtDate.HasValue && s.ClosedAtTime.HasValue
                    ? s.ClosedAtDate.Value.ToDateTime(s.ClosedAtTime.Value)
                    : (DateTime?)null));
    }
}

#region Reassignment
public static class ReassignmentMapper
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