namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class WarehouseDto
    {
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string? WarehouseCode { get; set; }
    }
}