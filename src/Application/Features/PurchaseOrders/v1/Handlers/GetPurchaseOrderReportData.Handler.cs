using AutoMapper;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Services;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrderReportDataHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper)
        : BaseValidatorHandler<GetPurchaseOrderReportDataQuery, PurchaseOrderTemplateDto>(unitOfWork, errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<PurchaseOrderTemplateDto> Handle(
            GetPurchaseOrderReportDataQuery request,
            CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var purchaseOrder = await unitOfWork.PurchaseOrders.Entities
                .Include(po => po.Supplier)
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
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return errorManager.ThrowNotFound<PurchaseOrderTemplateDto>(
                    "No se encontro la orden de compra",
                    "ERP:NOT_FOUND");
            }

            var template = mapper.Map<PurchaseOrderTemplateDto>(purchaseOrder);
            var culture = new CultureInfo("es-NI");
            var taxMetadata = TryParseTaxMetadata(purchaseOrder.Comments);

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
                        ?? Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

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
            var incomeTax = taxMetadata?.IrAmount ?? 0m;
            var municipalTax = taxMetadata?.ImiAmount ?? 0m;

            var isPaymentRequest = request.PaymentMethod.HasValue;
            var title = isPaymentRequest
                ? PurchaseOrdersMapper.GetDocumentTitleByMethodPayment(request.PaymentMethod!.Value)
                : "Orden de compra";

            var paymentRequestCode = taxMetadata?.PaymentRequestCode;
            var requestCode = isPaymentRequest
                ? paymentRequestCode ?? purchaseOrder.Code ?? purchaseOrder.PurchaseRequest.Code
                : purchaseOrder.Code ?? purchaseOrder.PurchaseRequest.Code;

            template.PurchaseOrderId = purchaseOrder.Id;
            template.PurchaseOrderCode = purchaseOrder.Code;
            template.PaymentMethod = request.PaymentMethod;
            template.PaymentRequestCode = paymentRequestCode;
            template.Tax = taxMetadata;
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

        private static PurchaseOrderTaxMetadata? TryParseTaxMetadata(string? comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                return null;
            }

            const string marker = "---TAX---";
            var markerIndex = comments.LastIndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var json = comments[(markerIndex + marker.Length)..].Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<PurchaseOrderTaxMetadata>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
