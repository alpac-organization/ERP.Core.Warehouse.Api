using System.Globalization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetPurchaseRequestDocumentHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper, IPdfGeneratorServices _pdfGeneratorServices, IS3StorageService _s3StorageService) : BaseValidatorHandler<GetPurchaseRequestDocumentQuery, PurchaseRequestDocumentDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseRequestDocumentDto> Handle(GetPurchaseRequestDocumentQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var template = request.DocumentType == PurchaseRequestType.Monthly
                ? await BuildMonthlyDocumentAsync(request, cancellationToken)
                : await BuildRequestDocumentAsync(request, cancellationToken);

            if (template is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseRequestDocumentDto>("No se encontró información para generar el documento", "ERP:NOT_FOUND");
            }

            var pdfBytes = await _pdfGeneratorServices.GenerateAsync<PurchaseRequestDocumentTemplateDto>("PurchaseRequestDocument", template);

            var fileName = $"{template.DocumentInfo.Title}-{template.DocumentInfo.RequestCode}.pdf";

            await using var pdfStream = new MemoryStream(pdfBytes);
            var documentUrl = await _s3StorageService.UploadPdfAsync("Compras", "SolicitudesCompras", pdfStream, fileName);

            return new PurchaseRequestDocumentDto
            {
                DocumentName = fileName,
                DocumentUrl  = documentUrl
            };
        }

        #region Documento mensual (consolidado)
        private async Task<PurchaseRequestDocumentTemplateDto?> BuildMonthlyDocumentAsync(GetPurchaseRequestDocumentQuery request, CancellationToken cancellationToken)
        {
            var year  = request.Year  ?? DateTime.UtcNow.Year;
            var month = request.Month ?? DateTime.UtcNow.Month;

            var firstDayOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            var purchaseRequests = await _unitOfWork.PurchaseRequests.Entities
                .Where(purs => purs.IsActive)
                .Include(purs => purs.Branch)
                    .ThenInclude(branch => branch.Company)
                .Include(purs => purs.WorkArea)
                .Include(purs => purs.PurchaseRequestItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.Category)
                .Include(purs => purs.PurchaseRequestItems)
                    .ThenInclude(item => item.UnitMeasure)
                .Where(purs => purs.RequestType == PurchaseRequestType.Monthly)
                .Where(purs => purs.Branch.CompanyId == request.CompanyId)
                .Where(purs => purs.CreatedAt >= firstDayOfMonth && purs.CreatedAt < firstDayOfNextMonth)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            if (purchaseRequests.Count == 0)
            {
                return null;
            }

            var culture = new CultureInfo("es-NI");
            var monthName = firstDayOfMonth.ToString("MMMM 'de' yyyy", culture).ToUpperInvariant();

            var areas = purchaseRequests
                .GroupBy(purs => purs.WorkArea?.WorkAreaName ?? purs.WorkArea?.Description ?? "Sin área")
                .Select(group => new PurchaseRequestDocumentArea
                {
                    AreaName   = group.Key,
                    Items      = group.SelectMany(purs => MapItems(purs.PurchaseRequestItems)).ToList()
                })
                .ToList();

            var company = purchaseRequests.FirstOrDefault()?.Branch.Company;
            var totalItems = areas.Sum(area => area.Items.Count);

            return new PurchaseRequestDocumentTemplateDto
            {
                Title             = $"Consolidado de Solicitudes Mensuales - {monthName}",
                Concept           = "Consolidado mensual de solicitudes de compras",
                CompanyInformation = MapCompany(company),
                DocumentInfo      = new DocumentInfo
                {
                    Title       = $"Consolidado mensual - {monthName}",
                    RequestCode = $"PERIODO {month:D2}-{year}",
                    Date        = DateTime.Now.ToString("dd/MM/yyyy", culture),
                    QuoteCount  = totalItems
                },
                Areas = areas
            };
        }
        #endregion

        #region Documento individual (Requisición / Eventual)
        private async Task<PurchaseRequestDocumentTemplateDto?> BuildRequestDocumentAsync(GetPurchaseRequestDocumentQuery request, CancellationToken cancellationToken)
        {
            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Include(purs => purs.Branch)
                    .ThenInclude(branch => branch.Company)
                .Include(purs => purs.WorkArea)
                .Include(purs => purs.PurchaseRequestItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.Category)
                .Include(purs => purs.PurchaseRequestItems)
                    .ThenInclude(item => item.UnitMeasure)
                .Where(purs => purs.Id == request.PurchaseRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return null;
            }

            var culture = new CultureInfo("es-NI");
            var title = request.DocumentType == PurchaseRequestType.Eventual
                ? "Solicitud de Compra Eventual"
                : "Solicitud de Compra - Requisición";

            var areaName = purchaseRequest.WorkArea?.WorkAreaName ?? purchaseRequest.WorkArea?.Description ?? "Sin área";

            return new PurchaseRequestDocumentTemplateDto
            {
                Title             = title,
                Concept           = purchaseRequest.Concept,
                CompanyInformation = MapCompany(purchaseRequest.Branch.Company),
                DocumentInfo      = new DocumentInfo
                {
                    Title       = title,
                    RequestCode = purchaseRequest.Code ?? purchaseRequest.Id.ToString(),
                    Date        = purchaseRequest.RequestDate.ToString("dd/MM/yyyy", culture),
                    QuoteCount  = purchaseRequest.PurchaseRequestItems.Count
                },
                Areas = new List<PurchaseRequestDocumentArea>
                {
                    new()
                    {
                        AreaName   = areaName,
                        RequestCode = purchaseRequest.Code,
                        Items      = MapItems(purchaseRequest.PurchaseRequestItems)
                    }
                }
            };
        }
        #endregion

        #region Helpers
        private static List<PurchaseRequestDocumentItem> MapItems(ICollection<PurchaseRequestItem> items)
        {
            return items
                .Select(item => new PurchaseRequestDocumentItem
                {
                    ProductName  = item.Product?.ProductName,
                    Description  = item.Description ?? item.Product?.Description,
                    Quantity     = item.Quantity,
                    QuantityUnit = item.QuantityUnit,
                    UnitMeasure  = item.UnitMeasure?.Name ?? item.UnitMeasure?.Symbol,
                    Category     = item.Product?.Category?.Name,
                    Justification = item.Justification
                })
                .ToList();
        }

        private CompanyInformation MapCompany(object? company)
        {
            if (company is null)
            {
                return new CompanyInformation();
            }

            var info = _mapper.Map<CompanyInformation>(company);
            return info ?? new CompanyInformation();
        }
        #endregion
    }
}
