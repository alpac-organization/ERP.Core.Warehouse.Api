namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{
    public class QuotationDto
    {
        public Guid QuotationId { get; set; }
        public string? MadeBy { get; set; }
        public DateOnly QuoteDate { get; set; }
        public string? QuotationCode { get; set; }
        public string? BranchName { get; set; }
        public string? Observations { get; set; }

        public List<QuotationDetailsDto> QuotationDetails { get; set; } = [];
    }
}