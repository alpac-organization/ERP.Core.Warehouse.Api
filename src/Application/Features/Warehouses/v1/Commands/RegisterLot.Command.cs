using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterLotCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    [JsonIgnore]
    public Guid SectionId { get; set; }

    public string Code { get; set; } = null!;
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public bool AllowsStacking { get; set; } = true;
    public RackStatus Status { get; set; } = RackStatus.Available;
    public string? UnavailableReason { get; set; }
}