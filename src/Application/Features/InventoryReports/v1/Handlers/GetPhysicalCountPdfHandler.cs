using MediatR;
using QuestPDF.Infrastructure;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

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
        private static readonly HttpClient LogoHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

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

            var generatedBy = access.User.Fullname ?? access.User.UserName ?? access.User.Email ?? "—";
            var logoImage = await LoadLogoAsync(access.Company.ImageUrl, ct);

            var bytes = pdfGenerator.Generate(
                PhysicalCountPdfComposer.Compose(lines, generatedBy, DateTime.Now, logoImage));

            return new PhysicalCountPdfResult
            {
                Bytes = bytes,
                CreatedAt = lines.PhysicalCount.CreatedAt
            };
        }

        private static async Task<Image?> LoadLogoAsync(string? logoUrl, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(logoUrl) ||
                !Uri.TryCreate(logoUrl, UriKind.Absolute, out var logoUri))
            {
                return null;
            }

            try
            {
                var bytes = await LogoHttpClient.GetByteArrayAsync(logoUri, ct);
                return Image.FromBinaryData(bytes);
            }
            catch (Exception)
            {
                // Si el logo no está disponible se genera el PDF sin él.
                return null;
            }
        }
    }
}
