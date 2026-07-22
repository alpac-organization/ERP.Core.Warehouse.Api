using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands
{
    public class RegisterQuoteCommand : BaseRequest, IRequest<bool>
    {
        public Guid BranchId { get; set; }
        public DateOnly QuoteDate { get; set; }
        public string? Observations { get; set; }
        public required List<QuoteDetails> QuoteDetails { get; set; } = [];
    }

    public class QuoteDetails
    {
        public bool IsNewProduct { get; set; } = false;
        public bool IsNewSupplier { get; set; } = false;

        public int Amount { get; set; }
        public Guid ProductId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid UnitMeasureId { get; set; }
        public string AdditionalData { get; set; } = "{}";

        public SupplierDatails? SupplierDatails { get; set; } = new ();
        public ProductInformation? ProductInformation { get; set; } = new();
    }

    public class ProductInformation
    {
        public Guid CategoryId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public ProductUsageType UsageType { get; set; }
    }

    public class SupplierDatails
    {
        public string SupplierName { get; set; } = null!;
        public string? ContactPhoneNumber { get; set; }

        public string? IdentificationNumber { get; set ; }
        public IdentificationType IdentificationType { get; set; }

        public ConstitutionType ConstitutionType { get; set; }
    }
}
