using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class PositionDetailDto
    {
        public OperationalOrderDetailsDto OperationalOrderDetail { get; set; } = new();
        public List<AssignmentStockPlacementsInformation> RemainingPositions { get; set; } = [];

        public string? CodeQr { get; set; }
        public string? CodeBar { get; set; }
        public string? QrCode { get; set; }
        public string? BarCode { get; set; }

        public AssignmentMerchandiseInformation? MerchandiseInformation { get; set; }
    }

    public class AssignmentMerchandiseInformation
    {
        public string? Merchandise { get; set; }
        public string? MerchandiseDescription { get; set; }
        public bool HasMerchandiseDescription { get; set; }
        public MerchandiseCategory? Category { get; set; }
        public UnloadingMerchandiseType MerchandiseType { get; set; }
        public DestinationType DestinationType { get; set; }
        public string? Observations { get; set; }
        public bool HasPositionatingInformation { get; set; }
        public List<PositionatingInformation> Pallets { get; set; } = [];
    }
}