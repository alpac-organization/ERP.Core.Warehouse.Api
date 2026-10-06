using MediatR;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands
{
    public class RegisterCoordinatesCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid WarehouseId { get; set; }

        [JsonIgnore]
        public Guid SectionId { get; set; }


        public Guid? LotId { get; set; }
        public Guid? RackId { get; set; }

        public CoordinateTargetType TargetType { get; set; }


        public List<LotsPositionsInformation> LotsPositionsInformation { get; set; } = [];
        public List<RackPositionsInformation> RackPositionsInformation { get; set; } = [];
    }

    public class LotsPositionsInformation
    {
        public Guid LotPositionId { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public decimal RotationY { get; set; }
    }

    public class RackPositionsInformation
    {
        public Guid RackPositionId { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public decimal RotationY { get; set; }
    }
}