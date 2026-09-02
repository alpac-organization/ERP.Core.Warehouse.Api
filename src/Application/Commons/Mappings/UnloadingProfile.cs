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
            .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => s.EntranceDucat!.ServiceOrderCode))
            .ForMember(d => d.WarehouseId, o => o.MapFrom(s => s.WarehouseId))
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
            .ForMember(d => d.Machinery, o => o.MapFrom(s => s.MachineryAssignments));

        CreateMap<MachineryAssignments, MachineryAssignmentDto>()
            .ForMember(d => d.Code, o => o.MapFrom(s => s.Machinery != null ? s.Machinery.Code : null));
        #endregion
    }
}