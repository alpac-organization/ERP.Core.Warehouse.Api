using System.Text.Json;

using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public static class QuotationAttachmentReader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public static QuotationAttachmentsDto Read(string? additionalData)
        {
            if (string.IsNullOrWhiteSpace(additionalData))
            {
                return QuotationAttachmentsDto.Empty;
            }

            try
            {
                var data = JsonSerializer.Deserialize<QuotationMetadataPayload>(additionalData, JsonOptions);
                var proforma = string.IsNullOrWhiteSpace(data?.ProformaNumber)
                    ? null
                    : data.ProformaNumber.Trim();
                return new QuotationAttachmentsDto(proforma, Map(data?.Images), Map(data?.Documents));
            }
            catch (JsonException)
            {
                return QuotationAttachmentsDto.Empty;
            }
        }

        private static List<QuotationFileDto> Map(IEnumerable<QuotationFileInformation>? files)
        {
            if (files is null)
            {
                return [];
            }

            return files
                .Select(file => new QuotationFileDto
                {
                    FileId = file.FileId,
                    FileName = file.FileName,
                    FileUrl = file.FileUrl,
                    UploadedAt = file.UploadedAt
                })
                .ToList();
        }
    }

    public sealed class QuotationMetadataPayload
    {
        public string? ProformaNumber { get; set; }
        public List<QuotationFileInformation> Images { get; set; } = [];
        public List<QuotationFileInformation> Documents { get; set; } = [];
    }

    public sealed record QuotationAttachmentsDto(
        string? ProformaNumber,
        List<QuotationFileDto> Images,
        List<QuotationFileDto> Documents)
    {
        public static QuotationAttachmentsDto Empty { get; } = new(null, [], []);
    }
}
