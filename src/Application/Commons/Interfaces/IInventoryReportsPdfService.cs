using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

public interface IInventoryReportsPdfService
{
    Task<byte[]> GenerateMovementReportPdfAsync(
        InventoryMovementReportDto report,
        string companyName,
        CompanyBranding branding,
        CancellationToken ct = default);

    Task<byte[]> GenerateTurnoverReportPdfAsync(
        InventoryTurnoverReportPdfModel model,
        CompanyBranding branding,
        CancellationToken ct = default);
}
