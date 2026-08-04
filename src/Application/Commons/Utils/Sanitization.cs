using System.Text.RegularExpressions;

namespace ERP.Core.Warehouse.Api.Application.Commons.Utils;

public static class SanitizationUtils
{
    private static readonly Regex NonAlphanumericPattern = new(@"[^a-zA-Z0-9À-ÿ\s\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesPattern = new(@"\s+", RegexOptions.Compiled);

    public static string SanitizeAlphanumeric(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var clean = value.Trim();
        clean = NonAlphanumericPattern.Replace(clean, string.Empty);
        clean = MultipleSpacesPattern.Replace(clean, " ");
        return clean.Trim();
    }

    public static List<string> SanitizeAlphanumericList(this IEnumerable<string>? values)
    {
        if (values == null) return [];
        return values
            .Select(v => v.SanitizeAlphanumeric())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    public static string SanitizeCode(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var clean = value.Trim();
        clean = NonAlphanumericPattern.Replace(clean, string.Empty); // quita símbolos no permitidos
        clean = clean.Replace(" ", string.Empty);
        return clean;
    }

    public static List<string> SanitizeCodeList(this IEnumerable<string>? values)
    {
        if (values == null) return [];
        return values
            .Select(v => v.SanitizeCode())
            .ToList();
    }
}