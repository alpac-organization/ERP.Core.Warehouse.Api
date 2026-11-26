using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Infrastructure.Documents.InventoryReports;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services;

public sealed class InventoryReportsPdfService(
    IQuestPdfGeneratorServices questPdfGeneratorServices) : IInventoryReportsPdfService
{
    public Task<byte[]> GenerateMovementReportPdfAsync(
        InventoryMovementReportDto report,
        string companyName,
        CompanyBranding branding,
        CancellationToken ct = default)
    {
        var document = new InventoryMovementReportDocument(report, companyName, branding);
        return questPdfGeneratorServices.GenerateAsync(document, ct);
    }

    public Task<byte[]> GenerateTurnoverReportPdfAsync(
        InventoryTurnoverReportPdfModel model,
        CompanyBranding branding,
        CancellationToken ct = default)
    {
        var document = new InventoryTurnoverReportDocument(model, branding);
        return questPdfGeneratorServices.GenerateAsync(document, ct);
    }
}
