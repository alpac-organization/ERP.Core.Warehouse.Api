using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class AnnulPurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<AnnulPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(AnnulPurchaseRequestCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowForbidden<bool>("No tienes permiso para anular la solicitud de compra", "ERP:FORBIDDEN");
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Include(pur => pur.AccountingReview)
                .Include(pur => pur.ManagementReview)
                .Include(pur => pur.PurchaseOrder)
                .Include(pur => pur.PurchaseRequestItems)
                    .ThenInclude(item => item.Quotations)
                .Where(pur => pur.Id == request.PurchaseRequestId)
                .Where(pur => pur.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra no fue encontrada o ya fue anulada", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            // Validación: estado terminal
            if (purchaseRequest.RequestStatus is PurchaseRequestStatus.Rejected or PurchaseRequestStatus.Canceled or PurchaseRequestStatus.Finished)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra ya se encuentra en un estado terminal (rechazada, cancelada o finalizada)", "ERP:PURCHASE_REQUEST_ALREADY_TERMINATED");
            }

            // Validación: ya fue aprobada por gerencia o cuenta con orden de compra emitida
            if (purchaseRequest.PurchaseOrder is not null || purchaseRequest.ManagementReview?.Status == ManagementReviewStatus.Approved)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra ya fue aprobada por gerencia o cuenta con una orden de compra emitida y no puede ser anulada", "ERP:PURCHASE_REQUEST_ALREADY_APPROVED");
            }

            // Validación por rol
            if (access.Role?.RoleType == RoleType.Operator)
            {
                if (purchaseRequest.RegisteredByUserId != access.User.Id)
                {
                    return _errorManager.ThrowForbidden<bool>("Como operador, solo puedes anular tus propias solicitudes de compra", "ERP:FORBIDDEN");
                }

                // El operador no puede anular si la solicitud ya está en proceso de revisión por Finanzas o Gerencia
                if (purchaseRequest.RequestStatus == PurchaseRequestStatus.Revision ||
                    (purchaseRequest.AccountingReview is not null && purchaseRequest.AccountingReview.DeletedAt == null) ||
                    (purchaseRequest.ManagementReview is not null && purchaseRequest.ManagementReview.DeletedAt == null))
                {
                    return _errorManager.ThrowBadRequest<bool>("No puedes anular la solicitud mientras se encuentra en proceso de revisión por finanzas o gerencia", "ERP:PURCHASE_REQUEST_IN_REVIEW");
                }
            }
            else if (access.Role?.RoleType == RoleType.Manager)
            {
                // if (purchaseRequest.AreaId != access.User.AreaId)
                // {
                //     return _errorManager.ThrowForbidden<bool>("Solo puedes anular solicitudes pertenecientes a tu área", "ERP:FORBIDDEN");
                // }
            }

            var now = DateTime.UtcNow;

            purchaseRequest.RequestStatus = PurchaseRequestStatus.Rejected;
            purchaseRequest.AnnulmentReason = request.Reason;
            purchaseRequest.AnnulledByUserId = access.User.Id;
            purchaseRequest.DeletedAt = now;

            // Anular revisiones activas si existían
            if (purchaseRequest.AccountingReview is not null)
            {
                purchaseRequest.AccountingReview.Status = AccountingReviewStatus.Rejected;
                purchaseRequest.AccountingReview.DeletedAt = now;
                await _unitOfWork.PurchaseRequestsReviewedAccounting.UpdateAsync(purchaseRequest.AccountingReview);
            }

            if (purchaseRequest.ManagementReview is not null)
            {
                purchaseRequest.ManagementReview.Status = ManagementReviewStatus.Rejected;
                purchaseRequest.ManagementReview.DeletedAt = now;
                await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(purchaseRequest.ManagementReview);
            }

            // Desmarcar y anular lógicamente cotizaciones e items
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

            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
