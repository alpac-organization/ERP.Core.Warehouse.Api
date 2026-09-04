using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

/// <summary>
/// Crea un rack con uno o más niveles verticales en la misma posición X/Z.
/// Cada nivel genera una entidad Racks con LevelNumber distinto y PositionY apilado.
/// </summary>
public class RegisterRackCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SectionId { get; set; }

    /// <summary>Código base del estante (ej. EST-01). Cada nivel usará Code-L{n}.</summary>
    public string Code { get; set; } = null!;

    [JsonPropertyName("layout_transform_3d_dto")]
    public LayoutTransform3DDto? LayoutTransform3DDto { get; set; }

    /// <summary>Niveles apilados (mismo ancho/largo, distinto LevelNumber y altura).</summary>
    public List<RackLevelCommand> Levels { get; set; } = [];
}

public class RackLevelCommand
{
    public int LevelNumber { get; set; }
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public decimal HeightMetres { get; set; }
    public RackUsageProfile UsageProfile { get; set; }
    public int MaxPulleys { get; set; } = 2;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}
