using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class SendPurchaseRequestToAccountingReviewHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) :  BaseValidatorHandler<SendPurchaseRequestToReviewCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(SendPurchaseRequestToReviewCommand request, CancellationToken cancellationToken)
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
                .Include(pur => pur.AccountingReview)
                .Include(pur => pur.PurchaseRequestItems)
                    .ThenInclude(item => item.Quotations)
                .Where(pur => pur.Id == request.PurchaseRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra no fue encontrada", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            if (!purchaseRequest.IsActive)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra se encuentra inactiva", "ERP:PURCHASE_REQUEST_INACTIVE");
            }

            if (purchaseRequest.RequestStatus != PurchaseRequestStatus.Approved)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra no se encuentra en estado aprobada", "ERP:PURCHASE_REQUEST_NOT_PENDING");
            }

            if (purchaseRequest.AccountingReview is not null)
            {
                return _errorManager.ThrowBadRequest<bool>("La solicitud de compra ya fue enviada a revisión", "ERP:REVIEW_ALREADY_EXISTS");
            }

            var itemsWithoutQuotation = purchaseRequest.PurchaseRequestItems
                .Where(item => !item.Quotations.Any(quo => quo.IsActive))
                .ToList();

            if (itemsWithoutQuotation.Count > 0)
            {
                return _errorManager.ThrowBadRequest<bool>("Todos los productos solicitados deben tener al menos una cotización asociada", "ERP:ITEMS_WITHOUT_QUOTATION");
            }

            var purchaseRequestsReviewedAccountingEntity = PurchaseRequestsReviewedAccountingMapper.ToPurchaseRequestsReviewedAccountingEntity(request, access.User.Id);

            await _unitOfWork.PurchaseRequestsReviewedAccounting.RegisterPurchaseRequestsReviewedAccounting(purchaseRequestsReviewedAccountingEntity);

            purchaseRequest.RequestStatus = PurchaseRequestStatus.Revision;
            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);

            #region Obtener perfil del usuario que ha realizado la solicitud.

            var profile = await _unitOfWork.Profiles.Entities
                .Where(prof => prof.UserId == purchaseRequest.RegisteredByUserId)
                .Where(prof => prof.CompanyId == request.CompanyId)
                .Where(prof => prof.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (profile is null)
            {
                return _errorManager.ThrowNotFound<bool>("El perfil del usuario que ha realizado la solicitud no fue encontrado", "ERP:PROFILE_NOT_FOUND");
            }
            
            var devices = await _unitOfWork.Devices.Entities
                .Where(dev => dev.UserProfileId == profile.Id)
                .Where(dev => dev.IsActive)
                .ToListAsync(cancellationToken);

            #endregion

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
