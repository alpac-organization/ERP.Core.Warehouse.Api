using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class ProcessPurchaseOrderHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<ProcessPurchaseOrderCommand, bool>(_unitOfWork, _errorManager)
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
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para aprobar o rechazar la solicitud y procesar la orden de compra", "ERP:INVALID_ACCESS");
            }

            var review = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .Where(rev => rev.Id == request.RequisitionManagementReviewId)
                .Where(rev => rev.Status == ManagementReviewStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            if (review is null)
            {
                return _errorManager.ThrowNotFound<bool>("La revisión de gerencia no fue encontrada o no está en estado pendiente", "ERP:MANAGEMENT_REVIEW_NOT_FOUND");
            }

            review.ReviewedByUserId = access.User.Id;
            review.Comments = request.Comments;

            switch (request.NewStatus)
            {
                case ManagementReviewStatus.Approved:
                {
                    review.Status = ManagementReviewStatus.Approved;

                    await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(review);

                    var purchaseOrder = review.ToPurchaseOrderEntity(access.User.Id);
                    await _unitOfWork.PurchaseOrders.RegisterPurchaseOrder(purchaseOrder);
                    break;
                }
                case ManagementReviewStatus.Rejected:
                {
                    review.Status = ManagementReviewStatus.Rejected;

                    await _unitOfWork.PurchaseRequestsReviewedManagement.UpdateAsync(review);
                    break;
                }
                default:
                    return _errorManager.ThrowBadRequest<bool>("El nuevo estado de la revisión no es válido", "ERP:INVALID_STATUS_CHANGE");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
