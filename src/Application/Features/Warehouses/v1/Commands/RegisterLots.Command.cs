using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Database.Domain.Enums;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterLotsCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SectionId { get; set; }
    public List<LotPlacementCommand> PlacementsLots { get; set; } = [];
}
public class LotPlacementCommand
{
    public string Code { get; set; } = null!;
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public int NominalRows { get; set; }
    public int NominalColumns { get; set; }
    public bool AllowsStacking { get; set; } = true;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public LayoutTransform3DDto? LayoutTransform3DDto { get; set; }
    public string? UnavailableReason { get; set; }
}
