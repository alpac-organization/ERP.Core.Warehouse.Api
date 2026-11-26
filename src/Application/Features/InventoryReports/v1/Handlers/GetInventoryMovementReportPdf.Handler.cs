using MediatR;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

public class GetInventoryMovementReportPdfHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMediator mediator,
    IInventoryReportsPdfService pdfService)
    : BaseValidatorHandler<GetInventoryMovementReportPdfQuery, ReportPdfFileDto>(unitOfWork, errorManager)
{
    public override async Task<ReportPdfFileDto> Handle(GetInventoryMovementReportPdfQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var report = await mediator.Send(new GetInventoryMovementReportQuery
        {
            CompanyId = request.CompanyId,
            ModuleCode = request.ModuleCode,
            UserId = request.UserId,
            From = request.From,
            To = request.To,
            BranchId = request.BranchId,
            CostCenterId = request.CostCenterId,
            ProductId = request.ProductId,
            ProductCode = request.ProductCode
        }, ct);

        var companyName = !string.IsNullOrWhiteSpace(access.Company.CompanieName)
            ? access.Company.CompanieName.Trim()
            : (!string.IsNullOrWhiteSpace(access.Company.Alias) ? access.Company.Alias.Trim() : "ALPAC, S.A.");

        var pdfBytes = await pdfService.GenerateMovementReportPdfAsync(report, companyName, ct);
        var fileName = $"reporte-movimientos-inventario-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.pdf";

        return new ReportPdfFileDto(pdfBytes, fileName);
    }
}
