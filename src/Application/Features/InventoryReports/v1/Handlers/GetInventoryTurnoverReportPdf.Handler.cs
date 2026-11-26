using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

public class GetInventoryTurnoverReportPdfHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMediator mediator,
    IInventoryReportsPdfService pdfService)
    : InventoryReportHandlerBase<GetInventoryTurnoverReportPdfQuery, ReportPdfFileDto>(unitOfWork, errorManager)
{
    public override async Task<ReportPdfFileDto> Handle(GetInventoryTurnoverReportPdfQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>("El periodo no es valido.", "ERP:010");

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<ReportPdfFileDto>("Debe especificar una sucursal para consultar la rotacion de inventario.", "ERP:012");

        if (isAdministrator && request.BranchId.HasValue)
        {
            var branchBelongsToCompany = await _unitOfWork.Branches.Entities
                .AsNoTracking()
                .AnyAsync(b => b.Id == request.BranchId.Value && b.CompanyId == request.CompanyId && b.DeletedAt == null, ct);

            if (!branchBelongsToCompany)
                return _errorManager.ThrowBadRequest<ReportPdfFileDto>("La sucursal seleccionada no pertenece a la empresa actual.", "ERP:013");
        }

        var items = await mediator.Send(new GetInventoryTurnoverReportQuery
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

        var generatedBy = access.User?.Fullname ?? await GetGeneratedByAsync(request.UserId, ct);

        var model = new InventoryTurnoverReportPdfModel
        {
            CompanyName = companyName,
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From.Value,
            To = request.To.Value,
            Items = items
        };

        var branding = await CompanyBrandingResolver.ResolveAsync(access.Company, ct);
        var pdfBytes = await pdfService.GenerateTurnoverReportPdfAsync(model, branding, ct);
        var fileName = $"reporte-rotacion-inventario-{request.From.Value:yyyyMMdd}-{request.To.Value:yyyyMMdd}.pdf";

        return new ReportPdfFileDto(pdfBytes, fileName);
    }
}
