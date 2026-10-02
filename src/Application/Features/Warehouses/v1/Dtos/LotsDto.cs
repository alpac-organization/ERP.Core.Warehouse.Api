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
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal Area { get; set; }
    public decimal? PositionX { get; set; }
    public decimal? PositionY { get; set; }
    public decimal? PositionZ { get; set; }
    public decimal? RotationY { get; set; }
}