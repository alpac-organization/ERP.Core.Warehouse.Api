using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Services;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class ProcessPurchaseOrderHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        ICodeGenerator codeGenerator,
        IPurchaseTaxService purchaseTaxService)
        : BaseValidatorHandler<ProcessPurchaseOrderCommand, bool>(unitOfWork, errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<bool> Handle(ProcessPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor || access.Role?.RoleType == RoleType.Operator)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "No tienes permiso para aprobar o rechazar la solicitud y procesar la orden de compra",
                    "ERP:INVALID_ACCESS");
            }

            var review = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q => q.IsActive && q.DeletedAt == null && q.IsAcceptedForPurchase))
                            .ThenInclude(q => q.Supplier)
                                .ThenInclude(s => s.SupplierDetails)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q => q.IsActive && q.DeletedAt == null && q.IsAcceptedForPurchase))
                            .ThenInclude(q => q.SupplierProduct)
                .Where(rev => rev.Id == request.RequisitionManagementReviewId)
                .Where(rev => rev.Status == ManagementReviewStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            if (review is null)
            {
                return _errorManager.ThrowNotFound<bool>(
                    "La revisión de gerencia no fue encontrada o no está en estado pendiente",
                    "ERP:MANAGEMENT_REVIEW_NOT_FOUND");
            }

            review.ReviewedByUserId = access.User.Id;
            review.Comments = request.Comments;

            switch (request.NewStatus)
            {
                case ManagementReviewStatus.Approved:
                {
                    review.Status = ManagementReviewStatus.Approved;
                    await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(review);

                    var acceptedQuotations = review.PurchaseRequest.PurchaseRequestItems
                        .SelectMany(item => item.Quotations)
                        .Where(q => q.IsActive && q.IsAcceptedForPurchase && q.DeletedAt == null)
                        .ToList();

                    if (acceptedQuotations.Count == 0)
                    {
                        return _errorManager.ThrowBadRequest<bool>(
                            "No hay cotizaciones aceptadas para generar la orden de compra",
                            "ERP:NO_ACCEPTED_QUOTATIONS");
                    }

                    var rates = await purchaseTaxService.GetActiveRatesAsync(cancellationToken);
                    var quotationsBySupplier = acceptedQuotations.GroupBy(q => q.SupplierId).ToList();

                    foreach (var supplierGroup in quotationsBySupplier)
                    {
                        var supplierQuotations = supplierGroup.ToList();
                        var firstQuote = supplierQuotations.First();
                        var supplierDetails = firstQuote.Supplier?.SupplierDetails;
                        var currency = firstQuote.SupplierProduct?.Currency ?? Currency.NIO;

                        var subtotal = supplierQuotations.Sum(q => q.PriceTotal);
                        var ivaTotal = supplierQuotations.Sum(q => q.Iva);

                        var retention = purchaseTaxService.CalculateRetentions(
                            subtotal,
                            ivaTotal,
                            currency,
                            supplierDetails?.SupplierType,
                            supplierDetails?.IsTaxExempt ?? false,
                            rates);

                        var paymentMethod = firstQuote.PaymentMethodType;
                        var (paymentCodeSucceeded, paymentRequestCode) =
                            await codeGenerator.GenerateUniquePaymentRequestCodeAsync(
                                review.PurchaseRequest.BranchId,
                                paymentMethod,
                                cancellationToken);

                        var taxMetadata = new PurchaseOrderTaxMetadata
                        {
                            ExchangeRate = retention.ExchangeRate,
                            TotalNio = retention.TotalNio,
                            ImiAmount = retention.ImiAmount,
                            IrAmount = retention.IrAmount,
                            ImiRate = retention.ImiRate,
                            IrRate = retention.IrRate,
                            RetentionsApplied = retention.RetentionsApplied,
                            SupplierType = retention.SupplierType,
                            IrTaxType = retention.IrTaxType,
                            PaymentRequestCode = paymentCodeSucceeded ? paymentRequestCode : null
                        };

                        var taxJson = JsonSerializer.Serialize(taxMetadata, JsonOptions);
                        var comments = string.IsNullOrWhiteSpace(request.Comments)
                            ? $"---TAX---{taxJson}"
                            : $"{request.Comments}\n---TAX---{taxJson}";

                        var (codeSucceeded, poCode) = await codeGenerator.GenerateUniquePurchaseOrderCode(
                            review.PurchaseRequest.BranchId, cancellationToken);

                        var purchaseOrder = review.ToPurchaseOrderEntity(access.User.Id);
                        purchaseOrder.SupplierId = supplierGroup.Key;
                        purchaseOrder.Comments = comments;

                        if (codeSucceeded && !string.IsNullOrWhiteSpace(poCode))
                        {
                            purchaseOrder.Code = poCode;
                        }

                        await _unitOfWork.PurchaseOrders.RegisterPurchaseOrder(purchaseOrder);

                        foreach (var quote in supplierQuotations)
                        {
                            var orderItem = new PurchaseOrderItem
                            {
                                Id = Guid.NewGuid(),
                                PurchaseOrderId = purchaseOrder.Id,
                                ProductId = quote.PurchaseRequestItem.ProductId,
                                PurchaseRequestItemId = quote.PurchaseRequestItemId,
                                Quantity = quote.PurchaseRequestItem.Quantity,
                                UnitPrice = quote.PriceUnit
                            };

                            await _unitOfWork.PurchaseOrderItems.RegisterPurchaseOrderItem(orderItem);
                        }
                    }

                    break;
                }
                case ManagementReviewStatus.Rejected:
                {
                    review.Status = ManagementReviewStatus.Rejected;
                    await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(review);
                    break;
                }
                default:
                    return _errorManager.ThrowBadRequest<bool>(
                        "El nuevo estado de la revisión no es válido", "ERP:INVALID_STATUS_CHANGE");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
