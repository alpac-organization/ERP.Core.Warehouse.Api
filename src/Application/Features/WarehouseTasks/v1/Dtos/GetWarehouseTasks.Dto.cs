using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Dtos;

public class WarehouseTaskDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public WarehouseTaskType TaskType { get; set; }
    public Guid SourceId { get; set; }
    public WarehouseTaskStatus Status { get; set; }
    public string? CurrentOwnerUserId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
