using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class ProcessPurchaseOrderHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        ICodeGenerator codeGenerator)
        : BaseValidatorHandler<ProcessPurchaseOrderCommand, bool>(unitOfWork, errorManager)
    {
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
                .Where(rev => rev.Id == request.RequisitionManagementReviewId
                           && rev.Status == ManagementReviewStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            if (review is null)
            {
                return _errorManager.ThrowNotFound<bool>(
                    "La revisión de gerencia no fue encontrada o no está en estado pendiente",
                    "ERP:MANAGEMENT_REVIEW_NOT_FOUND");
            }

            switch (request.NewStatus)
            {
                case ManagementReviewStatus.Approved:
                {
                    var acceptedQuotations = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                        .AsNoTracking()
                        .Where(rev => rev.Id == review.Id)
                        .SelectMany(rev => rev.PurchaseRequest.PurchaseRequestItems
                            .SelectMany(item => item.Quotations
                                .Where(q => q.IsActive && q.DeletedAt == null && q.IsAcceptedForPurchase)
                                .Select(q => new
                                {
                                    rev.PurchaseRequest.BranchId,
                                    q.SupplierId,
                                    q.PaymentMethodType,
                                    q.PriceUnit,
                                    q.PurchaseRequestItemId,
                                    item.ProductId,
                                    item.Quantity
                                })))
                        .ToListAsync(cancellationToken);

                    if (acceptedQuotations.Count == 0)
                    {
                        return _errorManager.ThrowBadRequest<bool>(
                            "No hay cotizaciones aceptadas para generar la orden de compra",
                            "ERP:NO_ACCEPTED_QUOTATIONS");
                    }

                    var branchId = acceptedQuotations[0].BranchId;
                    var quotationsBySupplier = acceptedQuotations.GroupBy(q => q.SupplierId).ToList();
                    var purchaseRequestId = review.PurchaseRequestId;
                    var reviewerUserId = access.User.Id;

                    try
                    {
                        await _unitOfWork.ExecuteWithUniqueCodeRetryAsync(async token =>
                        {
                            var reviewToUpdate = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                                .Where(rev => rev.Id == request.RequisitionManagementReviewId
                                           && rev.Status == ManagementReviewStatus.Pending)
                                .FirstOrDefaultAsync(token);

                            if (reviewToUpdate is null)
                            {
                                throw new PurchaseCodeGenerationException(
                                    "ERP:MANAGEMENT_REVIEW_NOT_FOUND",
                                    "La revisión de gerencia no fue encontrada o no está en estado pendiente");
                            }

                            reviewToUpdate.ReviewedByUserId = reviewerUserId;
                            reviewToUpdate.Comments = request.Comments;
                            reviewToUpdate.Status = ManagementReviewStatus.Approved;
                            await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(reviewToUpdate);

                            foreach (var supplierGroup in quotationsBySupplier)
                            {
                                var supplierQuotations = supplierGroup.ToList();
                                var paymentMethod = supplierQuotations[0].PaymentMethodType;
                                string? paymentRequestCode = null;

                                if (request.GeneratePaymentRequest)
                                {
                                    var (paymentCodeSucceeded, generatedPaymentCode) =
                                        await codeGenerator.GenerateUniquePaymentRequestCodeAsync(
                                            branchId,
                                            paymentMethod,
                                            token);

                                    if (!paymentCodeSucceeded || string.IsNullOrWhiteSpace(generatedPaymentCode))
                                    {
                                        throw new PurchaseCodeGenerationException(
                                            "ERP:ERROR_PAYMENT_REQUEST_CODE_GENERATOR",
                                            "Ocurrió un error al generar el código de la solicitud de pago");
                                    }

                                    paymentRequestCode = generatedPaymentCode;
                                }

                                var (codeSucceeded, poCode) = await codeGenerator.GenerateUniquePurchaseOrderCode(
                                    branchId, token);

                                var purchaseOrder = reviewToUpdate.ToPurchaseOrderEntity(reviewerUserId);
                                purchaseOrder.SupplierId = supplierGroup.Key;

                                if (codeSucceeded && !string.IsNullOrWhiteSpace(poCode))
                                {
                                    purchaseOrder.Code = poCode;
                                }

                                if (request.GeneratePaymentRequest && !string.IsNullOrWhiteSpace(paymentRequestCode))
                                {
                                    purchaseOrder.PaymentRequest = new PaymentRequest
                                    {
                                        Id = Guid.NewGuid(),
                                        Code = paymentRequestCode,
                                        PaymentMethodType = paymentMethod,
                                        PurchaseOrderId = purchaseOrder.Id
                                    };
                                }

                                await _unitOfWork.PurchaseOrders.RegisterPurchaseOrder(purchaseOrder);

                                foreach (var quote in supplierQuotations)
                                {
                                    var orderItem = new PurchaseOrderItem
                                    {
                                        Id = Guid.NewGuid(),
                                        PurchaseOrderId = purchaseOrder.Id,
                                        ProductId = quote.ProductId,
                                        PurchaseRequestItemId = quote.PurchaseRequestItemId,
                                        Quantity = quote.Quantity,
                                        UnitPrice = quote.PriceUnit
                                    };

                                    await _unitOfWork.PurchaseOrderItems.RegisterPurchaseOrderItem(orderItem);
                                }
                            }

                            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                                .Where(pr => pr.Id == purchaseRequestId)
                                .FirstOrDefaultAsync(token);

                            if (purchaseRequest is null)
                            {
                                throw new PurchaseCodeGenerationException(
                                    "ERP:PURCHASE_REQUEST_NOT_FOUND",
                                    "La solicitud de compra no fue encontrada");
                            }

                            purchaseRequest.RequestStatus = PurchaseRequestStatus.Finished;
                            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);

                            await _unitOfWork.SaveChangesAsync(token);
                        }, cancellationToken);
                    }
                    catch (PurchaseCodeGenerationException ex)
                    {
                        return ex.ErrorCode switch
                        {
                            "ERP:MANAGEMENT_REVIEW_NOT_FOUND" or "ERP:PURCHASE_REQUEST_NOT_FOUND"
                                => _errorManager.ThrowNotFound<bool>(ex.Message, ex.ErrorCode),
                            _ => _errorManager.ThrowBadRequest<bool>(ex.Message, ex.ErrorCode)
                        };
                    }

                    return true;
                }
                case ManagementReviewStatus.Rejected:
                {
                    review.ReviewedByUserId = access.User.Id;
                    review.Comments = request.Comments;
                    review.Status = ManagementReviewStatus.Rejected;
                    await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(review);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return true;
                }
                default:
                    return _errorManager.ThrowBadRequest<bool>(
                        "El nuevo estado de la revisión no es válido", "ERP:INVALID_STATUS_CHANGE");
            }
        }

        private sealed class PurchaseCodeGenerationException(string errorCode, string message)
            : InvalidOperationException(message)
        {
            public string ErrorCode { get; } = errorCode;
        }
    }
}
