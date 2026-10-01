using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterLotsCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    [JsonIgnore]
    public Guid SectionId { get; set; }

    public List<RegisterLotItem> Lots { get; set; } = [];
}

public class RegisterLotItem
{
    public int? NominalRows { get; set; }

    public int? NominalColumns { get; set; }

    public decimal? Width { get; set; }

    public decimal? Length { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public decimal? PositionZ { get; set; }

    public decimal? RotationY { get; set; }
}
