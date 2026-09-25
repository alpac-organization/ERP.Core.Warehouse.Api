using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class UpdateRackCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    [JsonIgnore]
    public Guid SectionId { get; set; }

    [JsonIgnore]
    public Guid RackId { get; set; }

    public int? RowNumber { get; set; }
    public RackUsageProfile? UsageProfile { get; set; }
    public RackStatus? Status { get; set; }
    public string? UnavailableReason { get; set; }

    // Medidas
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Height { get; set; }

    // Coordenadas
    public decimal? PositionX { get; set; }
    public decimal? PositionY { get; set; }
    public decimal? PositionZ { get; set; }
    public decimal? RotationY { get; set; }
}
