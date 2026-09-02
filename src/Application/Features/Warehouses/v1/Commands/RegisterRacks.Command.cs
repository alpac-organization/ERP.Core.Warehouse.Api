using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RacksBulkCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SectionId { get; set; }

    public List<RackPlacementCommand> PlacementRacks { get; set; } = [];
}
public class RackPlacementCommand
{

    public string Code { get; set; } = null!;
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal HeightMetres { get; set; }

    public RackUsageProfile UsageProfile { get; set; }
    public int RowNumber { get; set; }
    public int LevelNumber { get; set; }
    public int MaxPulleys { get; set; } = 2;

    public RackStatus Status { get; set; } = RackStatus.Available;

    public LayoutTransform3DDto? LayoutTransform3DDto { get; set; }
    public string? UnavailableReason { get; set; }
}