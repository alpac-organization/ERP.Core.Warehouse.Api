using System;
using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class WarehouseAssignmentsProfile : Profile
    {
        public WarehouseAssignmentsProfile()
        {
            // Command -> Database Entity (para ser consumido por UnitOfWork sin ensuciar el handler)
            CreateMap<CreateWarehouseAssignmentCommand, WarehouseAssignmentEntity>()
                .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
                .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.ReceptionId))
                .ForMember(d => d.WarehouseKeeperUserId, o => o.MapFrom(s => s.WarehouseChiefUserId))
                .ForMember(d => d.AssignedByUserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.AssignedAt, o => o.MapFrom(_ => NicaraguaClock.Now))
                .ForMember(d => d.UnloadingStartTime, o => o.MapFrom(_ => NicaraguaClock.Now));

            CreateMap<CreateUnloadingMachineryCommand, MachineryAssignments>()
                .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
                .ForMember(d => d.AssignedByUserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.StartTime, o => o.MapFrom(s => s.StartTime != default ? s.StartTime : NicaraguaClock.Now));

            CreateMap<CreateUnloadingCrewCommand, CrewAssignments>()
                .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
                .ForMember(d => d.AssignedAt, o => o.MapFrom(_ => NicaraguaClock.Now));
        }
    }
}

