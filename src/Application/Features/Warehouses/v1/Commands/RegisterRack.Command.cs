using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterRacksBulkCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SectionId { get; set; }
}

public class RackPlacementCommand
{
    public string Code { get; set; } = null!;

    public List<RackLevelCommand> Levels { get; set; } = [];
}

public class RackLevelCommand
{
    public int LevelNumber { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public RackUsageProfile UsageProfile { get; set; }
    public int MaxPulleys { get; set; } = 2;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}
