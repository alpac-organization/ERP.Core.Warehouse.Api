using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class CreateLotCoordinatesCommand : BaseRequest, IRequest<bool>, IHasCoordinates
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }

    [JsonIgnore]
    public Guid SectionId { get; set; }

    [JsonIgnore]
    public Guid LotId { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public decimal PositionZ { get; set; }

    public decimal RotationY { get; set; }
}