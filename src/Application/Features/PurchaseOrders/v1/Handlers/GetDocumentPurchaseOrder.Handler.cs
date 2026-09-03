using System.Globalization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetDocumentPurchaseOrderHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper, IPdfGeneratorServices _pdfGeneratorServices, IS3StorageService _s3StorageService) : BaseValidatorHandler<GetDocumentPurchaseOrderQuery, PurchaseOrderDocumentDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseOrderDocumentDto> Handle(GetDocumentPurchaseOrderQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }
            
            //Incluir información de mapeo
            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .Include(purs => purs.SentByUser)
                    .ThenInclude(user => user.WorkArea)

                .Include(purs => purs.ReviewedByUser)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                        .ThenInclude(branch => branch.Company)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                        .ThenInclude(area => area.CostCenters)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                        .ThenInclude(user => user.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)
                        .ThenInclude(user => user.WorkArea)

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
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderDocumentDto>("No se encontro la orden de compra", "ERP:NOT_FOUND");
            }

            var template = _mapper.Map<PurchaseOrderTemplateDto>(purchaseOrder);

            switch (request.PaymentMethod)
            {
                case PaymentMethod.Check:
                case PaymentMethod.BankTransfer:
                {
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

                    var culture = new CultureInfo("es-NI");
                    var serviceAmount = quotations.Sum(quotation => quotation.PriceTotal);
                    var vatAmount = quotations.Sum(quotation => quotation.Iva);

                    template.DocumentInfo = new DocumentInfo
                    {
                        Title       = PurchaseOrdersMapper.GetDocumentTitleByMethodPayment(request.PaymentMethod!.Value),
                        RequestCode = purchaseOrder.PurchaseRequest.Code,
                        Date        = DateTime.Now.ToString("dd/MM/yyyy", culture),
                        QuoteCount  = quotations.Count
                    };

                    template.PaymentInfo = new PaymentInfo
                    {
                        Department    = purchaseOrder.PurchaseRequest.WorkArea?.WorkAreaName ?? purchaseOrder.PurchaseRequest.WorkArea?.Description,
                        Payee         = quotations.FirstOrDefault()?.Supplier?.SuppliersLegalName,
                        Customer      = purchaseOrder.PurchaseRequest.Branch.Company?.CompanieName,
                        ServiceAmount = serviceAmount,
                        Vat           = vatAmount,
                        NetToPay      = serviceAmount + vatAmount
                    };

                    var pdfBytes = await _pdfGeneratorServices.GenerateAsync<PurchaseOrderTemplateDto>(
                        "PaymentRequestTemplate",
                        template);

                    var fileName = $"{template.DocumentInfo.Title}-{template.DocumentInfo.RequestCode}.pdf";

                    await using var pdfStream = new MemoryStream(pdfBytes);
                    var documentUrl = await _s3StorageService.UploadPdfAsync("Compras", "OrdenesCompra", pdfStream, fileName);

                    return new PurchaseOrderDocumentDto
                    {
                        DocumentName = fileName,
                        DocumentUrl  = documentUrl
                    };
                }
                case PaymentMethod.Credit:
                {
                    //Cuentas por pagar: lógica pendiente (queda vacía de forma intencional).

                    break;
                }
            }

            return new PurchaseOrderDocumentDto
            {
                DocumentName = "",
                DocumentUrl  = ""
            };
        }
    }
}
