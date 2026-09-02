using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

public class UnloadingAssignmentDetailDto
{
    public Guid AssignmentId { get; set; }
    public Guid RecordEntranceId { get; set; }
    public Guid? EntranceDucatId { get; set; }
    public string? WarehouseName { get; set; }
    public UnloadingStatus UnloadingStatus { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? WarehouseKeeperUserId { get; set; }
    public string? WarehouseKeeperUserName { get; set; }

    public List<MachineryAssignmentDto> Machinery { get; set; } = [];
    public CrewSummaryDto Crew { get; set; } = new();
}

public class MachineryAssignmentDto
{
    public string? Code { get; set; }
}

public class CrewSummaryDto
{
    public bool IsOutsourced { get; set; }
    public int PersonCount { get; set; }
    public List<string> MemberNames { get; set; } = [];
}