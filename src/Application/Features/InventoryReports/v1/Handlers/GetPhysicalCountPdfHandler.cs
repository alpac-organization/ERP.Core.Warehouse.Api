using MediatR;
using QuestPDF.Infrastructure;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Pdf;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers
{
    public class GetPhysicalCountPdfHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMediator mediator,
        IQuestPdfGeneratorServices pdfGenerator)
        : BaseValidatorHandler<GetPhysicalCountPdfQuery, PhysicalCountPdfResult>(unitOfWork, errorManager)
    {
        public override async Task<PhysicalCountPdfResult> Handle(GetPhysicalCountPdfQuery request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(
                request.UserId,
                request.CompanyId,
                request.ModuleCode!,
                ct);

            if (!access.IsSuccess)
                return access.ErrorResponse!;

            var lines = await mediator.Send(new GetPhysicalCountLinesQuery
            {
                CompanyId = request.CompanyId,
                ModuleCode = request.ModuleCode,
                UserId = request.UserId,
                PhysicalCountId = request.PhysicalCountId,
                PageNumber = 1,
                PageSize = int.MaxValue
            }, ct);

            if (lines.PhysicalCount.Status != PhysicalCountStatus.Completed)
            {
                return _errorManager.ThrowBadRequest<PhysicalCountPdfResult>(
                    "El levantamiento aún no está completado.",
                    "ERP:PHYSICAL_COUNT_NOT_COMPLETED");
            }

            var createdBy = lines.PhysicalCount.CreatedByName ?? "—";
            var createdAt = lines.PhysicalCount.CreatedAt;

            var branding = await CompanyBrandingResolver.ResolveAsync(access.Company, ct);
            var logoImage = branding.Logo is { Length: > 0 }
                ? Image.FromBinaryData(branding.Logo)
                : null;

            var bytes = pdfGenerator.Generate(
                PhysicalCountPdfComposer.Compose(lines, createdBy, createdAt, logoImage));

            return new PhysicalCountPdfResult
            {
                Bytes = bytes,
                CreatedAt = createdAt
            };
        }
    }
}
