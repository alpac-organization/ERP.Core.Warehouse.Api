using Microsoft.Extensions.Caching.Memory;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Infrastructure.Documents.InventoryReports;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services;

public sealed class InventoryReportsPdfService(
    IQuestPdfGeneratorServices questPdfGeneratorServices,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache) : IInventoryReportsPdfService
{
    public async Task<byte[]> GenerateMovementReportPdfAsync(
        InventoryMovementReportDto report,
        CompanyBrandingPdfModel branding,
        CancellationToken ct = default)
    {
        await EnrichBrandingAsync(branding, ct);
        var document = new InventoryMovementReportDocument(report, branding);
        return await questPdfGeneratorServices.GenerateAsync(document, ct);
    }

    public async Task<byte[]> GenerateTurnoverReportPdfAsync(
        InventoryTurnoverReportPdfModel model,
        CancellationToken ct = default)
    {
        await EnrichBrandingAsync(model.Branding, ct);
        var document = new InventoryTurnoverReportDocument(model);
        return await questPdfGeneratorServices.GenerateAsync(document, ct);
    }

    private async Task EnrichBrandingAsync(CompanyBrandingPdfModel branding, CancellationToken ct)
    {
        var (primary, headerBg, subtotalBg) = ResolveCompanyPalette(
            branding.CompanyAlias,
            branding.CompanyCode,
            branding.CompanyName);

        branding.PrimaryColor = primary;
        branding.HeaderBackgroundColor = headerBg;
        branding.SubtotalBackgroundColor = subtotalBg;

        if (branding.LogoBytes is null && !string.IsNullOrWhiteSpace(branding.CompanyLogoUrl))
        {
            branding.LogoBytes = await TryLoadLogoBytesAsync(branding.CompanyLogoUrl, ct);
        }
    }

    private static (string Primary, string HeaderBg, string SubtotalBg) ResolveCompanyPalette(
        string? alias,
        string? code,
        string? name)
    {
        var key = $"{alias} {code} {name}".ToUpperInvariant();

        if (key.Contains("AMINSA") || key.Contains("AMS") || key.Contains("MARITIMA") || key.Contains("MARÍTIMA"))
            return ("#006837", "#CDE1D7", "#E6F0EB");

        if (key.Contains("AVASA") || key.Contains("AVS") || key.Contains("VASSALLI") || key.Contains("VASSALI"))
            return ("#008542", "#CCE8D9", "#E6F3EC");

        if (key.Contains("VIGEMSA") || key.Contains("VGM") || key.Contains("VIGILANCIA"))
            return ("#1B2C56", "#D1D6E0", "#E8EAEE");

        if (key.Contains("TMN") || key.Contains("MULTIMODAL"))
            return ("#00803E", "#CCF0DE", "#E6F6EE");

        // Default: ALPAC
        return ("#004F9E", "#CDDCEB", "#E6EDF5");
    }

    private async Task<byte[]?> TryLoadLogoBytesAsync(string logoUrl, CancellationToken ct)
    {
        var cacheKey = $"company-pdf-logo:{logoUrl}";
        if (memoryCache.TryGetValue(cacheKey, out byte[]? cached) && cached is { Length: > 0 })
            return cached;

        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var logoUri))
            return null;

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var bytes = await client.GetByteArrayAsync(logoUri, ct);
            if (bytes is { Length: > 0 })
            {
                memoryCache.Set(cacheKey, bytes, TimeSpan.FromMinutes(30));
                return bytes;
            }
        }
        catch
        {
            // Si no se puede descargar el logo, el documento se genera igualmente sin interrumpir el reporte.
        }

        return null;
    }
}
