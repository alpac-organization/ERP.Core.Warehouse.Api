using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class SectionDto
    {
        public Guid SectionId { get; set; }
        public string? SectionCode { get; set; }
        public SectionType? SectionType { get; set; }
        public SectionStorageType? SectionStorageType { get; set; }
        public bool IsActive { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? PositionZ { get; set; }
        public decimal? RotationY { get; set; }
    }
}
