using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos
{
    public class MerchandiseLocationDetailsDto
    {
        public DateTime CreatedAt { get; set; }
        public DestinationType DestinationType { get; set; }
        public string? Merchandise { get; set; } = null!;
        public string? MerchandiseDescription { get; set; } = null!;

        public WarehouseInformation? WarehouseInformation { get; set; }
        public OperationalOrderInformation? OperationalOrderInformation { get; set; }

        public List<AssignmentStockPlacementsInformation> AssignmentStockPlacementsInformation { get; set; } = [];
    }

    public class OperationalOrderInformation
    {
        public string? PoCode { get; set; }
        public decimal? PackagesCount { get; set; }
        public Guid OperationalOrderId { get; set; }
        public bool IsAlerted { get; set; }
    }

    public class AssignmentStockPlacementsInformation
    {
        public SectionInformation? SectionInformation { get; set; } 
        public LotPositionInformation? LotPositionInformation { get; set; }
        public RackLocationInformation? RackPositionInformation { get; set; }
    }
    
    public class SectionInformation
    {
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
        public SectionType SectionType { get; set; }
        public SectionStorageType SectionStorageType { get; set; }
    }

    public class PositionBase
    {
        public string? PositionCode { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Level { get; set; }
    }

    public class RackLocationInformation : PositionBase
    {
        public RackStatus Status { get; set; }
    }

    public class LotPositionInformation : PositionBase
    {
        public RackStatus Status { get; set; }    
    }
}