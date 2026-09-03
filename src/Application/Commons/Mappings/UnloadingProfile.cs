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
        #region Queue
        CreateMap<WarehouseAssignments, AssignmentQueueItemDto>()
            .ForMember(d => d.AssignmentId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.RecordEntranceId))
            .ForMember(d => d.DucatNumber, o => o.MapFrom(s => s.EntranceDucat!.DucatNumber))
            .ForMember(d => d.DucatId, o => o.MapFrom(s => s.EntranceDucatId))
            .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => s.EntranceDucat!.ServiceOrderCode))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.WarehouseName))
            .ForMember(d => d.UnloadingStatus, o => o.MapFrom(s => s.UnloadingStatus));
        #endregion

        #region Detalle de asignación
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

        #region Iniciar descarga
        CreateMap<StartUnloadingCommand, UnloadingDetails>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.WarehouseAssignmentId, o => o.MapFrom(s => s.AssignmentId))
            .ForMember(d => d.MerchandiseType, o => o.MapFrom(s => s.MerchandiseType));

        CreateMap<StartUnloadingPalletItem, UnloadingPallets>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.UnloadingDetailsId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["UnloadingDetailsId"]))
            .ForMember(d => d.PalletType, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.LengthMetres, o => o.MapFrom((s, d, m, ctx) => s.Type == PalletType.Oversized ? s.LengthMetres : null))
            .ForMember(d => d.WidthMetres, o => o.MapFrom((s, d, m, ctx) => s.Type == PalletType.Oversized ? s.WidthMetres : null));

        CreateMap<StartUnloadingSupplyItem, UnloadingSupplies>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.UnloadingDetailsId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["UnloadingDetailsId"]))
            .ForMember(d => d.SuppliesId, o => o.MapFrom(s => s.SuppliesId));

        CreateMap<StartUnloadingCommand, StepExecutionLogs>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["RecordEntranceId"]))
            .ForMember(d => d.WorkflowStepDefinitionCode, o => o.MapFrom(_ => WorkflowStepCodes.Unloading))
            .ForMember(d => d.StartDate, o => o.MapFrom(s => s.StartDate))
            .ForMember(d => d.StartTime, o => o.MapFrom(s => s.StartTime))
            .ForMember(d => d.ProcessedByUserId, o => o.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.ProcessedByUserName, o => o.MapFrom((src, dest, destMember, ctx) => (string)ctx.Items["ProcessedByUserName"]));
        #endregion

        #region Detalle de descarga
        CreateMap<UnloadingDetails, UnloadingDetailDto>()
            .ForMember(d => d.AssignmentId, o => o.MapFrom(s => s.WarehouseAssignment.Id))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.WarehouseAssignment.RecordEntranceId))
            .ForMember(d => d.UnloadingStatus, o => o.MapFrom(s => s.WarehouseAssignment.UnloadingStatus))
            .ForMember(d => d.UnloadingDetailsId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StartDate, o => o.MapFrom((src, dest, member, ctx) => ctx.Items["StartLog"] is StepExecutionLogs log ? log.StartDate : (DateOnly?)null))
            .ForMember(d => d.StartTime, o => o.MapFrom((src, dest, member, ctx) => ctx.Items["StartLog"] is StepExecutionLogs log ? log.StartTime : (TimeOnly?)null))
            .ForMember(d => d.Pallets, o => o.MapFrom(s => s.UnloadingPallets.Where(p => p.DeletedAt == null)))
            .ForMember(d => d.Supplies, o => o.MapFrom(s => s.UnloadingSupplies.Where(s => s.DeletedAt == null)))
            .ForMember(d => d.ReservedPositions, o => o.MapFrom((src, dest, member, ctx) =>
                ctx.Items["Reservations"] is IEnumerable<UnloadingPositionReservations> reservations
                    ? reservations
                    : []));

        CreateMap<UnloadingPallets, UnloadingPalletDetailDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.PalletType));

        CreateMap<UnloadingSupplies, UnloadingSupplyDetailDto>()
            .ForMember(d => d.SupplyName, o => o.MapFrom(s => s.Supplies.Name));

        CreateMap<UnloadingPositionReservations, UnloadingPositionReservationDetailDto>()
            .ForMember(d => d.PositionCode, o => o.MapFrom((src, dest, member, ctx) =>
                ctx.Items["PositionCodes"] is Dictionary<Guid, string> codes
                    ? src.RackPositionId is Guid rackId
                        ? codes.GetValueOrDefault(rackId)
                        : src.LotPositionId is Guid lotId
                            ? codes.GetValueOrDefault(lotId)
                            : null
                    : null));
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