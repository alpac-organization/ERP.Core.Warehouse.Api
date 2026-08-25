using ERP.Core.Database.Domain.Enums;
namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class WarehouseDto
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public WarehouseType? WarehouseType { get; set; }
        public bool IsOwner { get; set; }
        public string? BranchCode { get; set; }
        public int SectionsCount { get; set; }
        public WarehouseCapacityDto? Capacity { get; set; }
        public bool HasChildren { get; set; }
    }

    public class WarehouseCapacityDto
    {
        public decimal TotalAreaM2 { get; set; }
        public decimal? UsableAreaM2 { get; set; }
        public decimal? UnusableAreaM2 { get; set; }
        public decimal? OccupiedAreaM2 { get; set; }
        public decimal? FreeAreaM2 { get; set; }
        public decimal? OccupancyPercentage { get; set; }
        public int TotalPositions { get; set; }
        public int UsedPositions { get; set; }
        public int FreePositions { get; set; }
        public DateTime? LastCalculatedAt { get; set; }
    }
}