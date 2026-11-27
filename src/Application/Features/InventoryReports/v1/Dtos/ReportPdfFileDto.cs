namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public sealed record ReportPdfFileDto(
    byte[] Content,
    string FileName,
    string ContentType = "application/pdf");

public sealed class CompanyBrandingPdfModel
{
    public required string CompanyName { get; init; }
    public string? CompanyAlias { get; init; }
    public string? CompanyCode { get; init; }
    public string? CompanyLogoUrl { get; init; }
    public byte[]? LogoBytes { get; set; }
    public string PrimaryColor { get; set; } = "#004F9E";
    public string HeaderBackgroundColor { get; set; } = "#CDDCEB";
    public string SubtotalBackgroundColor { get; set; } = "#E6EDF5";
}

public sealed class InventoryTurnoverReportPdfModel
{
    public required CompanyBrandingPdfModel Branding { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string? GeneratedBy { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public IReadOnlyList<InventoryTurnoverReportDto> Items { get; init; } = [];
}
