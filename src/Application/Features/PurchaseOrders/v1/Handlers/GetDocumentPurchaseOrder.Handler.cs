using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Enums;
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

            // Mapea la orden de compra al modelo del documento (información en inglés).
            var documentModel = _mapper.Map<PurchaseOrderDocumentTemplateDto>(purchaseOrder);

            // Valores que no provienen directamente de la entidad.
            documentModel.DocumentInfo.Title      = ResolveTitle(request.PaymentMethod);
            documentModel.DocumentInfo.IsNormal   = true;
            documentModel.DocumentInfo.IsCritical = false;

            // Genera el PDF a partir de la plantilla y los datos del modelo.
            var pdfBytes = await _pdfGeneratorServices.GenerateAsync<PurchaseOrderDocumentTemplateDto>("PaymentRequestTemplate", documentModel);

            await using var stream = new MemoryStream(pdfBytes);

            // Sube el PDF a S3 y retorna la URL pre-firmada hacia el frontend.
            var documentName = $"OC-{request.PurchaseOrderId}.pdf";
            var documentUrl = await _s3StorageService.UploadPdfAsync(
                "purchase-orders",
                "documents",
                stream,
                documentName);

            return new PurchaseOrderDocumentDto
            {
                DocumentName = documentName,
                DocumentUrl  = documentUrl
            };
        }

        private static string ResolveTitle(PaymentMethod? paymentMethod) => paymentMethod switch
        {
            PaymentMethod.BankTransfer => "SOLICITUD DE TRANSFERENCIA",
            PaymentMethod.Check        => "SOLICITUD DE CHEQUE",
            PaymentMethod.Credit       => "CUENTAS PENDIENTES",
            _                          => "SOLICITUD DE PAGO"
        };
    }
}
