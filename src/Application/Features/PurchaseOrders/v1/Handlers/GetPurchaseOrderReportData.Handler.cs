using AutoMapper;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrderReportDataHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper,
        IPurchaseRequestVisibilityService visibilityService)
        : BaseValidatorHandler<GetPurchaseOrderReportDataQuery, TransferRequestReportDto>(unitOfWork, errorManager)
    {
        public override async Task<TransferRequestReportDto> Handle(
            GetPurchaseOrderReportDataQuery request,
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
                .Include(po => po.Supplier)
                .Include(po => po.PaymentRequest)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(item => item.Product)
                .Include(po => po.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(po => po.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                        .ThenInclude(branch => branch.Company)
                .Include(po => po.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                .Include(po => po.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                .Include(po => po.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q =>
                            q.IsActive &&
                            q.DeletedAt == null &&
                            q.IsAcceptedForPurchase))
                            .ThenInclude(q => q.Supplier)
                .Include(po => po.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q =>
                            q.IsActive &&
                            q.DeletedAt == null &&
                            q.IsAcceptedForPurchase))
                            .ThenInclude(q => q.SupplierProduct)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(po => po.Id == request.PurchaseOrderId)
                .Where(po => po.DeletedAt == null)
                .Where(po => visibleRequestIds.Contains(po.PurchaseRequestId))
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowNotFound<TransferRequestReportDto>(
                    "No se encontro la orden de compra",
                    "ERP:NOT_FOUND");
            }

            var paymentRequestCode = purchaseOrder.PaymentRequest?.Code;
            if (string.IsNullOrWhiteSpace(paymentRequestCode))
            {
                return _errorManager.ThrowBadRequest<TransferRequestReportDto>(
                    "La solicitud de pago no tiene código.",
                    "ERP:PAYMENT_REQUEST_CODE_REQUIRED");
            }

            var header = mapper.Map<PurchaseOrderTemplateDto>(purchaseOrder);
            var supplierQuotations = PurchaseOrderReportLines.AcceptedQuotations(purchaseOrder);
            if (!PurchaseOrderReportLines.TryResolveCurrency(supplierQuotations, out var currency))
            {
                return _errorManager.ThrowBadRequest<TransferRequestReportDto>(
                    "La orden de compra tiene líneas en monedas distintas.",
                    "ERP:INCONSISTENT_PURCHASE_ORDER_CURRENCY");
            }

            var items = PurchaseOrderReportLines.BuildItems(purchaseOrder, supplierQuotations);

            var serviceAmount = items.Sum(item => item.PriceTotal);
            var ivaAmount = items.Sum(item => item.Iva);
            var incomeTax = supplierQuotations.Sum(quotation => quotation.Ir);
            var municipalTax = supplierQuotations.Sum(quotation => quotation.Imi);
            var culture = new CultureInfo("es-NI");
            const string title = "Solicitud de pago";

            return new TransferRequestReportDto
            {
                PurchaseOrderId = purchaseOrder.Id,
                PaymentMethod = request.PaymentMethod,
                PaymentRequestCode = paymentRequestCode,
                Title = title,
                Concept = header.Concept ?? purchaseOrder.PurchaseRequest.Concept,
                SentByUserInformation = header.SentByUserInformation,
                CompanyInformation = header.CompanyInformation,
                Items = items,
                DocumentInfo = new TransferRequestDocumentInfo
                {
                    Title = title,
                    RequestCode = paymentRequestCode,
                    Date = DateTime.Now.ToString("dd/MM/yyyy", culture)
                },
                PaymentInfo = new TransferRequestPaymentInfo
                {
                    Department = purchaseOrder.PurchaseRequest.WorkArea?.WorkAreaName
                        ?? purchaseOrder.PurchaseRequest.WorkArea?.Description,
                    Supplier = purchaseOrder.Supplier?.SuppliersLegalName
                        ?? supplierQuotations.FirstOrDefault()?.Supplier?.SuppliersLegalName,
                    Customer = purchaseOrder.PurchaseRequest.Branch.Company?.CompanieName,
                    Currency = currency,
                    ServiceAmount = serviceAmount,
                    Iva = ivaAmount,
                    Ir = incomeTax,
                    Imi = municipalTax,
                    NetToPay = serviceAmount + ivaAmount - incomeTax - municipalTax
                }
            };
        }
    }
}
