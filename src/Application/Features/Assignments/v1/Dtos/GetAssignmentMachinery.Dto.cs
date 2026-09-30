using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

public class GetAssignmentMachineryDto
{
    public Guid AssignmentMachineryId { get; set; }
    public Guid MachineryId { get; set; }
    public string? Concept { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedByUserName { get; set; } = default!;
    public Guid AssignmentOperationalId { get; set; }
    public MachineryInformation MachineryInformation { get; set; } = default!;
}

public class MachineryInformation
{
    public MachineryType MachineryType { get; set; }
    public string MachineryBrand { get; set; } = default!;
    public string MachineryCode { get; set; } = default!;
}
