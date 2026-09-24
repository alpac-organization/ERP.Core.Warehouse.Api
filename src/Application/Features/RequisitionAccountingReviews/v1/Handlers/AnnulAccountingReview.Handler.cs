using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Helpers;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Handlers
{
    public class AnnulAccountingReviewHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<AnnulAccountingReviewCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(AnnulAccountingReviewCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowForbidden<bool>("No tienes permiso para realizar esta acción", "ERP:FORBIDDEN");
            }

            var accountingReview = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseOrder)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.ManagementReview)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations)
                .Where(rev => rev.Id == request.RequisitionAccountingReviewId)
                .Where(rev => rev.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (accountingReview is null)
            {
                return _errorManager.ThrowNotFound<bool>("La revisión contable no fue encontrada o ya fue procesada", "ERP:ACCOUNTING_REVIEW_NOT_FOUND");
            }

            if (accountingReview.Status != AccountingReviewStatus.Pending)
            {
                return _errorManager.ThrowBadRequest<bool>("La revisión contable ya no se encuentra en estado pendiente", "ERP:INVALID_STATUS");
            }

            var purchaseRequest = accountingReview.PurchaseRequest;
            if (purchaseRequest is null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra asociada no fue encontrada", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            if (purchaseRequest.PurchaseOrder is not null || purchaseRequest.ManagementReview?.Status == ManagementReviewStatus.Approved)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud ya fue aprobada por gerencia o tiene una orden de compra emitida, no se puede anular ni retornar desde contabilidad", "ERP:PURCHASE_REQUEST_ALREADY_APPROVED");
            }

            if (request.Scope != AnnulmentScope.QuotationOnly && request.Scope != AnnulmentScope.FullProcess)
            {
                return _errorManager.ThrowBadRequest<bool>("El alcance de la anulación no es válido", "ERP:INVALID_SCOPE");
            }

            var now = DateTime.UtcNow;
            var isQuotationOnly = request.Scope == AnnulmentScope.QuotationOnly;

            accountingReview.ReviewedByUserId = access.User.Id;
            accountingReview.Status = isQuotationOnly ? AccountingReviewStatus.Returned : AccountingReviewStatus.Rejected;
            accountingReview.DeletedAt = now;

            RequisitionAnnulmentHelper.ApplyAnnulmentToPurchaseRequest(purchaseRequest, isQuotationOnly, request.Reason, access.User.Id, now);
            await RequisitionAnnulmentHelper.AnnulItemsAndQuotationsAsync(_unitOfWork, purchaseRequest, now);

            await _unitOfWork.PurchaseRequestsReviewedAccounting.UpdateAsync(accountingReview);
            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
