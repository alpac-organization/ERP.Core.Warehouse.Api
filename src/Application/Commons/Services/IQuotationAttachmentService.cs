using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public interface IQuotationAttachmentService
    {
        Task<(string? Json, string? ErrorMessage)> BuildAdditionalDataAsync(
            QuotationAttachmentsInput? attachments,
            string? existingJson,
            CancellationToken cancellationToken);
    }
}
