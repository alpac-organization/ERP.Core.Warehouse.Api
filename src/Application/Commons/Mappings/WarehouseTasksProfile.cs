using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class WarehouseTasksProfile : Profile
{
    public WarehouseTasksProfile()
    {
        CreateMap<WarehouseTask, WarehouseTaskDto>();

        CreateMap<PauseWarehouseTaskCommand, WarehouseTaskEvent>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.WarehouseTaskId, o => o.MapFrom((s, d, m, ctx) => (Guid)ctx.Items["WarehouseTaskId"]))
            .ForMember(d => d.EventType, o => o.MapFrom((s, d, m, ctx) => (WarehouseTaskEventType)ctx.Items["EventType"]))
            .ForMember(d => d.Status, o => o.MapFrom((s, d, m, ctx) => (WarehouseTaskStatus)ctx.Items["Status"]))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.OccurredAt, o => o.MapFrom((s, d, m, ctx) => (DateTime)ctx.Items["OccurredAt"]))
            .ForMember(d => d.Notes, o => o.Ignore())
            .ForMember(d => d.WarehouseTask, o => o.Ignore());

        CreateMap<ResumeWarehouseTaskCommand, WarehouseTaskEvent>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.WarehouseTaskId, o => o.MapFrom((s, d, m, ctx) => (Guid)ctx.Items["WarehouseTaskId"]))
            .ForMember(d => d.EventType, o => o.MapFrom((s, d, m, ctx) => (WarehouseTaskEventType)ctx.Items["EventType"]))
            .ForMember(d => d.Status, o => o.MapFrom((s, d, m, ctx) => (WarehouseTaskStatus)ctx.Items["Status"]))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.OccurredAt, o => o.MapFrom((s, d, m, ctx) => (DateTime)ctx.Items["OccurredAt"]))
            .ForMember(d => d.Notes, o => o.Ignore())
            .ForMember(d => d.WarehouseTask, o => o.Ignore());
    }
}
