namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{
    public class QuotedProductDto
    {
        public Guid QuotedProductId { get; set; }
        public bool IsWholesale { get; set; }

        public decimal PricePerUnit { get; set; }
        public decimal? PriceWholesale { get; set; }

        public int Quantity { get; set; }
        public int? EquivalentQuantity { get; set; }  
        public string? AdditionalData { get; set; } = "{}";

        // public Guid ProductId  { get; set; }
        // public Product Product { get; set; } = default!;

        // public Guid UnitOfMeasureId { get; set; }
        // public virtual UnitMeasure UnitMeasure { get; set; } = default!;

        // public Guid QuoteDetailId { get; set; }
        // public virtual QuoteDetail QuoteDetail { get; set; } = default!;
    }
}