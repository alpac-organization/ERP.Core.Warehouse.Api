using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Handlers
{
    public class AnnulManagementReviewHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<AnnulManagementReviewCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(AnnulManagementReviewCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor || access.Role?.RoleType == RoleType.Operator)
            {
                return _errorManager.ThrowForbidden<bool>("No tienes permiso para anular o retornar revisiones de gerencia", "ERP:FORBIDDEN");
            }

            var managementReview = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseOrder)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.AccountingReview)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations)
                .Where(rev => rev.Id == request.RequisitionManagementReviewId)
                .Where(rev => rev.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (managementReview is null)
            {
                return _errorManager.ThrowNotFound<bool>("La revisión de gerencia no fue encontrada o ya fue procesada", "ERP:MANAGEMENT_REVIEW_NOT_FOUND");
            }

            if (managementReview.Status != ManagementReviewStatus.Pending)
            {
                return _errorManager.ThrowBadRequest<bool>("La revisión de gerencia ya no se encuentra en estado pendiente", "ERP:INVALID_STATUS");
            }

            var purchaseRequest = managementReview.PurchaseRequest;
            if (purchaseRequest is null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra asociada no fue encontrada", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            // Validación: ya cuenta con orden de compra generada
            if (purchaseRequest.PurchaseOrder is not null)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud ya cuenta con una orden de compra generada, no se puede anular ni retornar", "ERP:PURCHASE_ORDER_ALREADY_EXISTS");
            }

            var now = DateTime.UtcNow;
            managementReview.ReviewedByUserId = access.User.Id;

            switch (request.Scope)
            {
                case AnnulmentScope.QuotationOnly:
                {
                    // 1. Gerencia rechaza/descarta esta revisión
                    managementReview.Status = ManagementReviewStatus.Rejected;
                    managementReview.DeletedAt = now;

                    // 2. Cascada a Finanzas: invalida la aprobación contable previa pasándola a Returned
                    if (purchaseRequest.AccountingReview is not null)
                    {
                        purchaseRequest.AccountingReview.Status = AccountingReviewStatus.Returned;
                        purchaseRequest.AccountingReview.DeletedAt = now;
                        await _unitOfWork.PurchaseRequestsReviewedAccounting.UpdateAsync(purchaseRequest.AccountingReview);
                    }

                    // 3. Desmarcar y anular lógicamente cotizaciones y resetear bandera en items
                    foreach (var item in purchaseRequest.PurchaseRequestItems)
                    {
                        item.HasQuotation = false;
                        await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);

                        foreach (var quote in item.Quotations)
                        {
                            quote.IsActive = false;
                            quote.IsAcceptedForPurchase = false;
                            quote.DeletedAt = now;
                            await _unitOfWork.Quotations.UpdateAsync(quote);
                        }
                    }

                    // 4. Reactivar la solicitud en compras (sigue viva para re-cotizar en Approved)
                    purchaseRequest.RequestStatus = PurchaseRequestStatus.Approved;
                    purchaseRequest.AnnulmentReason = request.Reason;
                    purchaseRequest.AnnulledByUserId = access.User.Id;
                    purchaseRequest.DeletedAt = null;
                    break;
                }
                case AnnulmentScope.FullProcess:
                {
                    // 1. Gerencia
                    managementReview.Status = ManagementReviewStatus.Rejected;
                    managementReview.DeletedAt = now;

                    // 2. Finanzas en cascada
                    if (purchaseRequest.AccountingReview is not null)
                    {
                        purchaseRequest.AccountingReview.Status = AccountingReviewStatus.Rejected;
                        purchaseRequest.AccountingReview.DeletedAt = now;
                        await _unitOfWork.PurchaseRequestsReviewedAccounting.UpdateAsync(purchaseRequest.AccountingReview);
                    }

                    // 3. Cotizaciones y items
                    foreach (var item in purchaseRequest.PurchaseRequestItems)
                    {
                        item.HasQuotation = false;
                        await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);

                        foreach (var quote in item.Quotations)
                        {
                            quote.IsActive = false;
                            quote.IsAcceptedForPurchase = false;
                            quote.DeletedAt = now;
                            await _unitOfWork.Quotations.UpdateAsync(quote);
                        }
                    }

                    // 4. Solicitud cancelada definitivamente
                    purchaseRequest.RequestStatus = PurchaseRequestStatus.Rejected;
                    purchaseRequest.AnnulmentReason = request.Reason;
                    purchaseRequest.AnnulledByUserId = access.User.Id;
                    purchaseRequest.DeletedAt = now;
                    break;
                }
                default:
                    return _errorManager.ThrowBadRequest<bool>("El alcance de la anulación no es válido", "ERP:INVALID_SCOPE");
            }

            await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(managementReview);
            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
