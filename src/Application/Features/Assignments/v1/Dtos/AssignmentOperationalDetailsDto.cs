namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class AssignmentOperationalDetailsDto : AssignmentOperationalDto
    {
        public string? Observations { get; set; }
        public string? AdditionalData { get; set; }
    }
}