using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Database.Domain.Enums;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

/// <summary>
/// Crea un único tramo (Lot). El frontend dibuja un rectángulo en el plano 2D
/// y envía sus medidas + transform; NominalRows/Columns configuran el interior.
/// </summary>
public class RegisterLotCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SectionId { get; set; }

    public string Code { get; set; } = null!;
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public int NominalRows { get; set; }
    public int NominalColumns { get; set; }
    public bool AllowsStacking { get; set; } = true;
    public RackStatus Status { get; set; } = RackStatus.Available;

    [JsonPropertyName("layout_transform_3d_dto")]
    public LayoutTransform3DDto? LayoutTransform3DDto { get; set; }

    public string? UnavailableReason { get; set; }
}
