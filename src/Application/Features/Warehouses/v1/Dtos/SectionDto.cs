using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class SectionDto
    {
        public Guid SectionId { get; set; }
        public string? SectionCode { get; set; }
        public string? SectionName { get; set; }
        public SectionType? SectionType { get; set; }
        public SectionStorageType? StorageType { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalAreaM2 { get; set; }
        public decimal UsedAreaM2 { get; set; }
        public int TotalPositions { get; set; }
        public int UsedPositions { get; set; }
    }
}
