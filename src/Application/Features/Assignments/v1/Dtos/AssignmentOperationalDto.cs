using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class AssignmentOperationalDto
    {
        public Guid AssignmentId { get; set; }
        public Guid OperationalOrderId { get; set; }
        public string? ReceptionCode { get; set; }
        public string? PoCode { get; set; }
        public bool IsAlerted { get; set; }
        public DestinationType DestinationType { get; set; }
        public AssignmentOperationalStatus Status { get; set; }

        public string? Merchandise { get; set; } = null!;
        public string? MerchandiseDescription { get; set; } = null!;

        public bool HasMachineryAssigned { get; set; }
        public bool HasCollaboratorsAssigned { get; set; }
        public bool HasOutsourcedCrew { get; set; }
        public bool HasOutsourcedMachinery { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}