using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public sealed record CompanyBranding(
    string PrimaryColor,
    string HeaderColor,
    string SubtotalColor,
    byte[]? Logo = null)
{
    public static CompanyBranding Default { get; } = new("#156082", "#CAEDFB", "#C0E6F5");
}

public static class CompanyBrandingPalette
{
    private sealed record Palette(string Primary, string Header, string Subtotal);

    private static readonly Palette Fallback = new("#156082", "#CAEDFB", "#C0E6F5");

    private static readonly Dictionary<string, Palette> ByAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ALPAC"] = new("#004f9e", "#e6edf5", "#cddceb"),
        ["AMINSA"] = new("#006837", "#e6f0eb", "#cde1d7"),
        ["AVASA"] = new("#008542", "#e6f3ec", "#e6f3ec"),
        ["VIGEMSA"] = new("#1b2c56", "#e8eaee", "#e8eaee"),
        ["TMN"] = new("#00a651", "#e6f6ee", "#e6f6ee")
    };

    public static CompanyBranding Resolve(string? alias, string? companyName = null)
    {
        if (TryResolve(alias, out var palette) || TryResolve(companyName, out palette))
            return new CompanyBranding(palette.Primary, palette.Header, palette.Subtotal);

        return CompanyBranding.Default;
    }

    private static bool TryResolve(string? value, out Palette palette)
    {
        palette = Fallback;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var key = value.Trim();

        if (ByAlias.TryGetValue(key, out var direct))
        {
            palette = direct;
            return true;
        }

        foreach (var (alias, candidate) in ByAlias)
        {
            if (key.Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                palette = candidate;
                return true;
            }
        }

        return false;
    }
}

public static class CompanyBrandingResolver
{
    public static async Task<CompanyBranding> ResolveAsync(Company company, CancellationToken ct)
    {
        var branding = CompanyBrandingPalette.Resolve(company.Alias, company.CompanieName);

        if (string.IsNullOrWhiteSpace(company.ImageUrl) ||
            !Uri.TryCreate(company.ImageUrl, UriKind.Absolute, out var logoUri))
            return branding;

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var logoBytes = await httpClient.GetByteArrayAsync(logoUri, ct);
            return branding with { Logo = logoBytes };
        }
        catch
        {
            return branding;
        }
    }
}
