using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

public class AssignmentQueueItemDto
{
    public Guid AssignmentId { get; set; }
    public Guid RecordEntranceId { get; set; }
    public string? DucatNumber { get; set; }
    public string? ServiceOrderCode { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public UnloadingStatus UnloadingStatus { get; set; }
}

public class GetAssignmentQueueDto
{
    public List<AssignmentQueueItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}