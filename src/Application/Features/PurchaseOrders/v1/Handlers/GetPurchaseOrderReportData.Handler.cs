using AutoMapper;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
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
        : BaseValidatorHandler<GetPurchaseOrderReportDataQuery, PurchaseOrderTemplateDto>(unitOfWork, errorManager)
    {
        public override async Task<PurchaseOrderTemplateDto> Handle(
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
                .AsNoTracking()
                .AsSplitQuery()
                .Where(po => po.Id == request.PurchaseOrderId)
                .Where(po => po.DeletedAt == null)
                .Where(po => visibleRequestIds.Contains(po.PurchaseRequestId))
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowNotFound<PurchaseOrderTemplateDto>(
                    "No se encontro la orden de compra",
                    "ERP:NOT_FOUND");
            }

            var template = mapper.Map<PurchaseOrderTemplateDto>(purchaseOrder);
            var culture = new CultureInfo("es-NI");

            var supplierQuotations = purchaseOrder.PurchaseRequest.PurchaseRequestItems
                .SelectMany(item => item.Quotations)
                .Where(q => q.IsActive && q.DeletedAt == null && q.IsAcceptedForPurchase)
                .Where(q => !purchaseOrder.SupplierId.HasValue || q.SupplierId == purchaseOrder.SupplierId)
                .ToList();

            var quotationsByRequestItem = supplierQuotations
                .GroupBy(q => q.PurchaseRequestItemId)
                .ToDictionary(g => g.Key, g => g.First());

            var items = purchaseOrder.PurchaseOrderItems
                .Select(orderItem =>
                {
                    Quotation? quotation = null;
                    if (orderItem.PurchaseRequestItemId.HasValue)
                    {
                        quotationsByRequestItem.TryGetValue(orderItem.PurchaseRequestItemId.Value, out quotation);
                    }

                    var unitPrice = orderItem.UnitPrice;
                    var quantity = orderItem.Quantity;
                    var priceTotal = quotation?.PriceTotal
                        ?? quantity * unitPrice;

                    return new PurchaseOrderReportItemDto
                    {
                        PurchaseOrderItemId = orderItem.Id,
                        PurchaseRequestItemId = orderItem.PurchaseRequestItemId,
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.Product?.ProductName,
                        ProductCode = orderItem.Product?.Code,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        PriceTotal = priceTotal,
                        Iva = quotation?.Iva ?? 0m
                    };
                })
                .ToList();

            var serviceAmount = items.Sum(i => i.PriceTotal);
            var vatAmount = items.Sum(i => i.Iva);
            var incomeTax = supplierQuotations.Sum(q => q.Ir);
            var municipalTax = supplierQuotations.Sum(q => q.Imi);

            var isPaymentRequest = request.PaymentMethod.HasValue;
            var title = isPaymentRequest
                ? "Solicitud de pago" 
                : "Orden de compra";

            var paymentRequestCode = purchaseOrder.PaymentRequest?.Code;

            if (isPaymentRequest && string.IsNullOrWhiteSpace(paymentRequestCode))
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderTemplateDto>(
                    "La solicitud de pago no tiene código.",
                    "ERP:PAYMENT_REQUEST_CODE_REQUIRED");
            }

            if (!isPaymentRequest && string.IsNullOrWhiteSpace(purchaseOrder.Code))
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderTemplateDto>(
                    "La orden de compra no tiene código.",
                    "ERP:PURCHASE_ORDER_CODE_REQUIRED");
            }

            var requestCode = isPaymentRequest
                ? paymentRequestCode
                : purchaseOrder.Code;

            template.PurchaseOrderId = purchaseOrder.Id;
            template.PurchaseOrderCode = purchaseOrder.Code;
            template.PaymentMethod = request.PaymentMethod;
            template.PaymentRequestCode = paymentRequestCode;
            template.Title = title;
            template.Concept ??= purchaseOrder.PurchaseRequest.Concept;
            template.Items = items;

            template.DocumentInfo = new DocumentInfo
            {
                Title = title,
                RequestCode = requestCode,
                Date = DateTime.Now.ToString("dd/MM/yyyy", culture),
                QuoteCount = supplierQuotations.Count
            };

            var payee = purchaseOrder.Supplier?.SuppliersLegalName
                ?? supplierQuotations.FirstOrDefault()?.Supplier?.SuppliersLegalName;

            template.PaymentInfo = new PaymentInfo
            {
                Department = purchaseOrder.PurchaseRequest.WorkArea?.WorkAreaName
                    ?? purchaseOrder.PurchaseRequest.WorkArea?.Description,
                Payee = payee,
                Customer = purchaseOrder.PurchaseRequest.Branch.Company?.CompanieName,
                ServiceAmount = serviceAmount,
                Vat = vatAmount,
                IncomeTax = incomeTax,
                MunicipalTax = municipalTax,
                NetToPay = serviceAmount + vatAmount - incomeTax - municipalTax
            };

            return template;
        }
    }
}
