using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class AssignmentDetailsDto : AssignmentDto
    {
        public EnclosureDto? Enclosure { get; set; }
        public List<MachineryDto> Machineries { get; set; } = [];
        public List<CollaboratorDto> Collaborators { get; set; } = [];
        public string? AdditionalData { get; set; }
    }

    public class EnclosureDto
    {
        public Guid EnclosureId { get; set; }
        public string? Observations { get; set; }
        public string Merchandise { get; set; } = default!;
        public string MerchandiseDescription { get; set; } = default!;
        public DestinationType DestinationType { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? WarehouseCode { get; set; }
    }

    public class MachineryDto
    {
        public Guid MachineryAssignmentId { get; set; }
        public string? Concept { get; set; }
        public Guid MachineryId { get; set; }
        public string MachineryCode { get; set; } = default!;
        public string MachineryName { get; set; } = default!;
        public bool IsActive { get; set; }
    }

    public class CollaboratorDto
    {
        public Guid CollaboratorAssignmentId { get; set; }
        public Guid CollaboratorId { get; set; }
        public string CollaboratorName { get; set; } = default!;
        public string CollaboratorIdentification { get; set; } = default!;
        public AssignmentCollaboratorsRoles Role { get; set; }
        public bool IsActive { get; set; }
    }
}