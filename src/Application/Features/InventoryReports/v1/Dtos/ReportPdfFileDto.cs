namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

public sealed record ReportPdfFileDto(
    byte[] Content,
    string FileName,
    string ContentType = "application/pdf");

public sealed class InventoryTurnoverReportPdfModel
{
    public required string CompanyName { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string? GeneratedBy { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public IReadOnlyList<InventoryTurnoverReportDto> Items { get; init; } = [];
}
