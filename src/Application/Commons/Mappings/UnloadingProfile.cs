using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class UnloadingProfile : Profile
{
    public UnloadingProfile()
    {
        #region Unloading - Queue
        CreateMap<WarehouseAssignments, AssignmentQueueItemDto>()
            .ForMember(d => d.AssignmentId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.RecordEntranceId))
            .ForMember(d => d.DucatNumber, o => o.MapFrom(s => s.EntranceDucat!.DucatNumber))
            .ForMember(d => d.DucatId, o => o.MapFrom(s => s.EntranceDucatId))
            .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => s.EntranceDucat!.ServiceOrderCode))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.WarehouseName))
            .ForMember(d => d.UnloadingStatus, o => o.MapFrom(s => s.UnloadingStatus));
        #endregion

        #region Unloading - Detalle de asignación
        CreateMap<WarehouseAssignments, UnloadingAssignmentDetailDto>()
            .ForMember(d => d.AssignmentId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.RecordEntranceId))
            .ForMember(d => d.EntranceDucatId, o => o.MapFrom(s => s.EntranceDucatId))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.WarehouseName))
            .ForMember(d => d.UnloadingStatus, o => o.MapFrom(s => s.UnloadingStatus))
            .ForMember(d => d.AssignedAt, o => o.MapFrom(s => s.AssignedAt))
            .ForMember(d => d.WarehouseKeeperUserId, o => o.MapFrom(s => s.WarehouseKeeperUserId))
            .ForMember(d => d.WarehouseKeeperUserName, o => o.MapFrom((s, d, m, ctx) => (string?)ctx.Items["WarehouseKeeperUserName"] ?? s.WarehouseKeeperUserId.ToString()))
            .ForMember(d => d.Machinery, o => o.MapFrom(s => s.MachineryAssignments))
            .ForMember(d => d.Crew, o => o.MapFrom((s, d, m, ctx) => BuildCrew(s.CrewAssignments, ctx.Items["CrewMemberNames"] as Dictionary<Guid, string> ?? new())));

        CreateMap<MachineryAssignments, MachineryAssignmentDto>()
            .ForMember(d => d.Code, o => o.MapFrom(s => s.Machinery != null ? s.Machinery.Code : null));
        #endregion
    }

    private static CrewSummaryDto BuildCrew(IEnumerable<CrewAssignments> crewAssignments, Dictionary<Guid, string> namesById)
    {
        var crewRows = crewAssignments.Where(c => c.DeletedAt == null).ToList();

        var outsourcedRows = crewRows.Where(c => c.IsOutsourced).ToList();
        if (outsourcedRows.Count > 0)
        {
            return new CrewSummaryDto
            {
                IsOutsourced = true,
                PersonCount = outsourcedRows.Sum(c => c.PersonCount ?? 0),
                MemberNames = []
            };
        }

        var collaboratorIds = crewRows
            .Where(c => c.CollaboratorId.HasValue)
            .Select(c => c.CollaboratorId!.Value)
            .ToList();

        return new CrewSummaryDto
        {
            IsOutsourced = false,
            PersonCount = collaboratorIds.Count,
            MemberNames = collaboratorIds
                .Select(id => namesById.GetValueOrDefault(id) ?? id.ToString())
                .ToList()
        };
    }
}