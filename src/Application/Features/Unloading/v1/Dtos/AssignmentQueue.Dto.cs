using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;

public class AssignmentQueueItemDto
{
    public Guid AssignmentId { get; set; }
    public Guid RecordEntranceId { get; set; }
    public string? DucatNumber { get; set; }
    public Guid DucatId { get; set; }
    public string? ServiceOrderCode { get; set; }
    public string? WarehouseName { get; set; }
    [JsonConverter(typeof(JsonNumberEnumConverter<UnloadingStatus>))]
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