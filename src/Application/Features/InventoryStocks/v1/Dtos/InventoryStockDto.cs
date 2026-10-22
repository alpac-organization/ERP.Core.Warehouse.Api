using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos
{
    public class InventoryStockDto
    {
        public int MinimumStock { get; set; }
        public int? MaximumStock { get; set; }
        public int AvailableQuantity { get; set; }
        public Guid ProductStockId { get; set; }

        public ProductInformation? ProductInformation { get; set; }
        public CostCenterInformation? CostCenterInformation { get; set;  }
    }

    public class ProductInformation
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public ProductUsageType ProductUsageType { get; set; }

        public CategoryInformation? CategoryInformation { get; set; }
    }

    public class CategoryInformation
    {
        public string? Code {get; set;}
        public string? Name {get; set;}
        public Guid CategoryId { get; set; }
    }
}