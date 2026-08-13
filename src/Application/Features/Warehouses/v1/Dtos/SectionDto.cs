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
    }
}
