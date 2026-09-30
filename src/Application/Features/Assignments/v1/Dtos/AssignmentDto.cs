using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class AssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public Guid OperationalOrderId { get; set; }
        public AssignmentOperationalStatus Status { get; set; }
        public bool HasMachineryAssigned { get; set; }
        public bool HasEnclosureAssigned { get; set; }
        public bool HasCollaboratorsAssigned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}