using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands
{
    public class RegisterQuoteCommand : BaseRequest, IRequest<bool>
    {
        public Guid BranchId { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public DateOnly QuoteDate { get; set; }
        public string? Observations { get; set; }

        public required List<QuoteDetails> QuoteDetails { get; set; } = [];
    }

    public class QuoteDetails
    {
        public Guid SupplierId { get; set; }
        public List<ProductInformation> Products { get; set; } = [];
    }

    public class ProductInformation
    {
        public Guid ProductId { get; set; }
        public Guid UnitOfMeasureId { get; set; }

        public bool IsWholesale { get; set; }

        public int Quantity { get; set; }
        public int? QuantityPerUnit { get; set; }
        public decimal PriceTotal { get; set; }

        public QuotedProductData AdditionalData { get; set; } = new();
    }
}
