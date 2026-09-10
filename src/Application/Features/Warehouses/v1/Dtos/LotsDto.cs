namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

using ERP.Core.Database.Domain.Enums;

public class LotListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public RackStatus Status { get; set; }
    public bool AllowsStacking { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime? StatusChangedAt { get; set; }
}