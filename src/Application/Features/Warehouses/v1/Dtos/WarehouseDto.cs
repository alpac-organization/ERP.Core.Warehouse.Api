using ERP.Core.Database.Domain.Enums;
namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class WarehouseDto
    {
        public Guid WarehouseId { get; set; }        
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public WarehouseType? WarehouseType { get; set; }        
    }

}