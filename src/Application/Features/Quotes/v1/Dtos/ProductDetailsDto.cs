using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{
    public class ProductDetailsDto
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public ProductUsageType UsageType { get; set; }

        public CategoryDetails CategoryDetails { get; set; } = new ();
    }

    public class CategoryDetails
    {
        public Guid CategoryId { get; set; }
        public Guid? ParentId { get; set; }
        public string? CategoryName {get; set;}
        public string? CategoryCode {get; set;}
    }
}