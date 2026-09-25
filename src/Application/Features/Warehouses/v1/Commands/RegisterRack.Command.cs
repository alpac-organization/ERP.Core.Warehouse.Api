using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterRacksBulkCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    [JsonIgnore]
    public Guid SectionId { get; set; }

    public int Quantity { get; set; }
    public int RowNumber { get; set; }
    public int LevelNumber { get; set; }
    public int MaxPulleys { get; set; }

    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal? Height { get; set; }

    public RackUsageProfile UsageProfile { get; set; }

    public decimal InitialPositionX { get; set; }
    public decimal InitialPositionY { get; set; }
    public decimal SpacingX { get; set; }
}
