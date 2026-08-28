using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterLotsCommand : BaseRequest
{
    public Guid SectionId { get; set; }
    public List<LotsCommand> Groups { get; set; } = [];
}
public class LotPlacementCommand
{
    public string Code { get; set; } = null!;


    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public LayoutTransform3DDto? LayoutTransform3DDto { get; set; }
    public bool AllowsStacking { get; set; } = true;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}

public class LotsCommand
{
    public List<LotPlacementCommand> Placements { get; set; } = [];
}