using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class SendPurchaseRequestToManagementReviewHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) :  BaseValidatorHandler<SendPurchaseRequestToManagementReviewCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(SendPurchaseRequestToManagementReviewCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Include(pur => pur.ManagementReview)
                .Include(pur => pur.PurchaseRequestItems)
                    .ThenInclude(item => item.Quotations)
                .Where(pur => pur.IsActive)
                .Where(pur => pur.Id == request.PurchaseRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra no fue encontrada", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }
            
            //Si la solicitud fue rechazada por contabilidad queda en estado rechazado y no puede enviarse a gerencia
            if (purchaseRequest.RequestStatus != PurchaseRequestStatus.Approved)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra debe estar aprobada para enviarse a revisión de gerencia", "ERP:PURCHASE_REQUEST_NOT_APPROVED");
            }

            //Solicitud enviada a gerencia
            if (purchaseRequest.ManagementReview is not null)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra ya fue enviada a revisión", "ERP:REVIEW_ALREADY_EXISTS");
            }

            var items = await _unitOfWork.PurchaseRequestItems.Entities
                .Include(item => item.Quotations)
                .Where(item => item.PurchaseRequestId == purchaseRequest.Id)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                var activeQuotations = item.Quotations
                    .Where(quo => quo.IsActive)
                    .ToList();

                if (activeQuotations.Count < 2)
                {
                    return _errorManager.ThrowBadRequest<bool>("Todos los productos solicitados deben tener al menos dos cotizaciones activas", "ERP:ITEMS_WITHOUT_TWO_QUOTATIONS");
                }

                if (activeQuotations.Count(quo => quo.IsAcceptedForPurchase) != 1)
                {
                    return _errorManager.ThrowBadRequest<bool>("Todos los productos solicitados deben tener una única cotización aceptada para compra", "ERP:ITEM_WITHOUT_ACCEPTED_QUOTATION");
                }
            }

            var requisitionManagementReviewEntity = RequisitionManagementReviewMapper.ToRequisitionManagementReviewEntity(request, access.User.Id);

            await _unitOfWork.RequisitionManagementReviews.RegisterRequisitionManagementReview(requisitionManagementReviewEntity);

            purchaseRequest.RequestStatus = PurchaseRequestStatus.Revision;

            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
    
}
