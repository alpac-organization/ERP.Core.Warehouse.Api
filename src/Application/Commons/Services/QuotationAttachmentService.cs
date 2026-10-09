using System.Text.Json;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public class QuotationAttachmentService(IS3StorageService s3StorageService) : IQuotationAttachmentService
    {
        private const int MaxImages = 2;
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private const long MaxPdfBytes = 10 * 1024 * 1024;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public async Task<(string? Json, string? ErrorMessage)> BuildAdditionalDataAsync(
            QuotationAttachmentsInput? attachments,
            string? existingJson,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(s3StorageService);

            if (attachments is null)
            {
                return (existingJson, null);
            }

            var images = attachments.ImagesBase64?
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList() ?? [];

            var hasPdf = !string.IsNullOrWhiteSpace(attachments.PdfBase64);

            if (images.Count == 0 && !hasPdf)
            {
                return (existingJson, null);
            }

            if (images.Count > MaxImages)
            {
                return (null, $"Solo se permiten hasta {MaxImages} imágenes por cotización.");
            }

            if (hasPdf && !string.IsNullOrWhiteSpace(attachments.PdfFileName)
                && !attachments.PdfFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return (null, "El documento adjunto debe ser un archivo PDF.");
            }

            var additionalData = string.IsNullOrWhiteSpace(existingJson)
                ? new QuotationAdditionalData()
                : JsonSerializer.Deserialize<QuotationAdditionalData>(existingJson, JsonOptions) ?? new QuotationAdditionalData();

            if (hasPdf)
            {
                if (additionalData.Documents.Count >= 1)
                {
                    additionalData.Documents.Clear();
                }

                var pdfBase64 = StripDataUrlPrefix(attachments.PdfBase64!);
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(pdfBase64);
                }
                catch (FormatException)
                {
                    return (null, "El PDF adjunto no tiene un formato Base64 válido.");
                }

                if (bytes.Length == 0 || bytes.Length > MaxPdfBytes)
                {
                    return (null, "El PDF adjunto es inválido o excede el tamaño permitido.");
                }

                await using var stream = new MemoryStream(bytes);
                var fileName = string.IsNullOrWhiteSpace(attachments.PdfFileName)
                    ? $"cotizacion-{Guid.NewGuid():N}.pdf"
                    : attachments.PdfFileName;

                var url = await s3StorageService.UploadPdfAsync("Compras", "Cotizaciones", stream, fileName);
                additionalData.Documents.Add(new QuotationFileInformation
                {
                    FileId = Guid.NewGuid(),
                    FileName = fileName,
                    FileUrl = url,
                    UploadedAt = DateTime.UtcNow
                });
            }

            if (images.Count > 0)
            {
                additionalData.Images.Clear();

                foreach (var image in images)
                {
                    var imageBase64 = StripDataUrlPrefix(image);
                    try
                    {
                        var imageBytes = Convert.FromBase64String(imageBase64);
                        if (imageBytes.Length == 0 || imageBytes.Length > MaxImageBytes)
                        {
                            return (null, "Una o más imágenes son inválidas o exceden el tamaño permitido.");
                        }
                    }
                    catch (FormatException)
                    {
                        return (null, "Una o más imágenes no tienen un formato Base64 válido.");
                    }

                    var url = await s3StorageService.UploadImageAsync(
                        "Compras",
                        "Cotizaciones",
                        imageBase64,
                        cancellationToken);

                    additionalData.Images.Add(new QuotationFileInformation
                    {
                        FileId = Guid.NewGuid(),
                        FileName = $"image-{Guid.NewGuid():N}.jpg",
                        FileUrl = url,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            return (JsonSerializer.Serialize(additionalData, JsonOptions), null);
        }

        private static string StripDataUrlPrefix(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var commaIndex = content.IndexOf(',');
            if (content.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            {
                return content[(commaIndex + 1)..];
            }

            return content;
        }
    }
}
