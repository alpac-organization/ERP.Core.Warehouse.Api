using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;
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
            .ForMember(d => d.WarehouseKeeperUserName, o => o.MapFrom((s, d, m, ctx) => (string?)ctx.Items["WarehouseKeeperUserName"] ?? s.WarehouseKeeperUserId))
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

#region Unloading - Iniciar descarga
public static class StartUnloadingMapper
{
    public static UnloadingDetails ToDetailsEntity(
        this StartUnloadingCommand command,
        DateOnly startDate,
        TimeOnly startTime)
    {
        return new UnloadingDetails
        {
            Id = Guid.NewGuid(),
            WarehouseAssignmentId = command.AssignmentId,
            MerchandiseType = command.MerchandiseType
        };
    }

    public static UnloadingPallets ToPalletEntity(
        this StartUnloadingPalletItem item,
        Guid unloadingDetailsId)
    {
        var isOversized = item.Type == PalletType.Oversized;

        return new UnloadingPallets
        {
            Id = Guid.NewGuid(),
            UnloadingDetailsId = unloadingDetailsId,
            PalletType = item.Type,
            Quantity = item.Quantity,
            LengthMetres = isOversized ? item.LengthMetres : null,
            WidthMetres = isOversized ? item.WidthMetres : null
        };
    }

    public static UnloadingSupplies ToSupplyEntity(
        this StartUnloadingSupplyItem item,
        Guid unloadingDetailsId)
    {
        return new UnloadingSupplies
        {
            Id = Guid.NewGuid(),
            UnloadingDetailsId = unloadingDetailsId,
            SuppliesId = item.SuppliesId,
            Quantity = item.Quantity
        };
    }

    public static StepExecutionLogs ToStepExecutionLogEntity(
        this StartUnloadingCommand command,
        Guid recordEntranceId,
        DateOnly startDate,
        TimeOnly startTime,
        string processedByUserId,
        string processedByUserName)
    {
        return new StepExecutionLogs
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = recordEntranceId,
            WorkflowStepDefinitionCode = WorkflowStepCodes.Unloading,
            StartDate = startDate,
            StartTime = startTime,
            ProcessedByUserId = processedByUserId,
            ProcessedByUserName = processedByUserName
        };
    }
}
#endregion