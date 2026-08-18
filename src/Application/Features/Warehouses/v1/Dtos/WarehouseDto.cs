using ERP.Core.Database.Domain.Enums;
namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class WarehouseDto
    {
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string? WarehouseCode { get; set; }
        public bool IsActive { get; set; }
        public WarehouseType? WarehouseType { get; set; }

        public List<WarehouseDto> SubWarehouses { get; set; } = [];
    }
}