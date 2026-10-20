using System.Text.Json;

using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    /// <summary>
    /// Lee las URLs de imágenes y PDF guardadas en el JSON de la cotización.
    /// </summary>
    public static class QuotationAttachmentReader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Deserializa los adjuntos ya subidos a almacenamiento. Un JSON vacío o inválido devuelve listas vacías.
        /// </summary>
        public static QuotationAttachmentsDto Read(string? additionalData)
        {
            if (string.IsNullOrWhiteSpace(additionalData))
            {
                return QuotationAttachmentsDto.Empty;
            }

            try
            {
                var data = JsonSerializer.Deserialize<QuotationAdditionalData>(additionalData, JsonOptions);
                return new QuotationAttachmentsDto(Map(data?.Images), Map(data?.Documents));
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

    /// <summary>
    /// Imágenes y PDF de una cotización, listos para el detalle.
    /// </summary>
    public sealed record QuotationAttachmentsDto(List<QuotationFileDto> Images, List<QuotationFileDto> Documents)
    {
        public static QuotationAttachmentsDto Empty { get; } = new([], []);
    }
}
