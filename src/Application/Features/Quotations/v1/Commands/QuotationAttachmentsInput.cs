namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands
{
    public class QuotationAttachmentsInput
    {
        public string? PdfBase64 { get; set; }

        public string? PdfFileName { get; set; }

        public List<string>? ImagesBase64 { get; set; }
    }
}
