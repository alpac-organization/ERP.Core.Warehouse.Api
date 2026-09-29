using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class AssignmentEnclosureData
    {
        public string? Observations { get; set; }
        public string Merchandise { get; set; } = default!;
        public string MerchandiseDescription { get; set; } = default!;
        public DestinationType DestinationType { get; set; }
        public Guid? WarehouseId { get; set; }
    }
}