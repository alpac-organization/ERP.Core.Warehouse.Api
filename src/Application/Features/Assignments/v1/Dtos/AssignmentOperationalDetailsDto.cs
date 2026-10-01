using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class AssignmentOperationalDetailsDto : AssignmentOperationalDto
    {
        public string? Observations { get; set; }
        public string? AdditionalData { get; set; }

        public WarehouseInformation? WarehouseInformation { get; set; }
    }


    public class WarehouseInformation
    {
        public Guid WarehouseId { get; set; }        
        public string Code { get; set; } = string.Empty;
        public WarehouseType WarehouseType { get; set; }        
    }
}