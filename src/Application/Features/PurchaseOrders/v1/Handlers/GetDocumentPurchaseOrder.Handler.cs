using AutoMapper;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetDocumentPurchaseOrderHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper,
        IPdfGeneratorServices pdfGeneratorServices,
        IS3StorageService s3StorageService,
        IPurchaseRequestVisibilityService visibilityService)
        : BaseValidatorHandler<GetDocumentPurchaseOrderQuery, PurchaseOrderDocumentDto>(unitOfWork, errorManager)
    {
        public override async Task<PurchaseOrderDocumentDto> Handle(
            GetDocumentPurchaseOrderQuery request,
            CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var visibleRequestIds = visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities,
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .Select(pr => pr.Id);

            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .Include(purs => purs.SentByUser)
                .Include(purs => purs.ReviewedByUser)
                .Include(purs => purs.PaymentRequest)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                        .ThenInclude(branch => branch.Company)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                        .ThenInclude(area => area.CostCenters)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations)
                            .ThenInclude(quotation => quotation.Supplier)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(purs => purs.Id == request.PurchaseOrderId)
                .Where(purs => visibleRequestIds.Contains(purs.PurchaseRequestId))
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderDocumentDto>(
                    "No se encontro la orden de compra",
                    "ERP:NOT_FOUND");
            }

            var template = mapper.Map<PurchaseOrderTemplateDto>(purchaseOrder);
            var culture = new CultureInfo("es-NI");

            var quotations = purchaseOrder.PurchaseRequest.PurchaseRequestItems
                .SelectMany(item => item.Quotations)
                .Where(quotation => quotation.IsActive && quotation.IsAcceptedForPurchase)
                .ToList();

            if (quotations.Count == 0)
            {
                quotations = purchaseOrder.PurchaseRequest.PurchaseRequestItems
                    .SelectMany(item => item.Quotations)
                    .Where(quotation => quotation.IsActive)
                    .ToList();
            }

            var serviceAmount = quotations.Sum(quotation => quotation.PriceTotal);
            var vatAmount = quotations.Sum(quotation => quotation.Iva);
            var incomeTax = quotations.Sum(quotation => quotation.Ir);
            var municipalTax = quotations.Sum(quotation => quotation.Imi);

            var isPaymentRequest = request.PaymentMethod.HasValue;
            var title = isPaymentRequest
                ? "Solicitud de pago" 
                : "Orden de compra";

            var paymentRequestCode = purchaseOrder.PaymentRequest?.Code;
            var requestCode = isPaymentRequest
                ? paymentRequestCode ?? purchaseOrder.Code ?? purchaseOrder.PurchaseRequest.Code
                : purchaseOrder.Code ?? purchaseOrder.PurchaseRequest.Code;

            template.DocumentInfo = new DocumentInfo
            {
                Title = title,
                RequestCode = requestCode,
                Date = DateTime.Now.ToString("dd/MM/yyyy", culture),
                QuoteCount = quotations.Count
            };

            template.PaymentInfo = new PaymentInfo
            {
                Department = purchaseOrder.PurchaseRequest.WorkArea?.WorkAreaName
                    ?? purchaseOrder.PurchaseRequest.WorkArea?.Description,
                Payee = quotations.FirstOrDefault()?.Supplier?.SuppliersLegalName,
                Customer = purchaseOrder.PurchaseRequest.Branch.Company?.CompanieName,
                ServiceAmount = serviceAmount,
                Vat = vatAmount,
                IncomeTax = incomeTax,
                MunicipalTax = municipalTax,
                NetToPay = serviceAmount + vatAmount - incomeTax - municipalTax
            };

            var pdfBytes = await pdfGeneratorServices.GenerateAsync<PurchaseOrderTemplateDto>(
                "PaymentRequestTemplate",
                template);

            var fileName = $"{template.DocumentInfo.Title}-{template.DocumentInfo.RequestCode}.pdf";

            await using var pdfStream = new MemoryStream(pdfBytes);
            var documentUrl = await s3StorageService.UploadPdfAsync("Compras", "OrdenesCompra", pdfStream, fileName);

            return new PurchaseOrderDocumentDto
            {
                DocumentName = fileName,
                DocumentUrl = documentUrl
            };
        }
    }
}
