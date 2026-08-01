using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class ProcessPurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<ProcessPurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(ProcessPurchaseRequestCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor || access.Role?.RoleType == RoleType.Operator)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Where(x => x.Id == request.PurchaseRequestId)
                .Where(x => x.RequestStatus == PurchaseRequestStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest == null)
            {
                return _errorManager.ThrowNotFound<bool>("La solicitud de compra no fue encontrada o no está en estado pendiente", "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            switch (request.NewStatus)
            {
                case PurchaseRequestStatus.Approved:
                {
                    //Verificamos quien aprobo la solicitud de compra
                    purchaseRequest.UserRevisionId = access.User.Id;
                    purchaseRequest.RequestStatus = request.NewStatus;
                    purchaseRequest.RevisionDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    
                    await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
                    break;
                }
                case PurchaseRequestStatus.Rejected:
                {
                    purchaseRequest.UserRevisionId = access.User.Id;
                    purchaseRequest.RequestStatus = request.NewStatus;
                    purchaseRequest.ReasonRejection = request.ReasonRejection;
                    purchaseRequest.RevisionDate = DateOnly.FromDateTime(DateTime.UtcNow);

                    await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
                    break;   
                }
                case PurchaseRequestStatus.Canceled:
                {
                    purchaseRequest.UserRevisionId = access.User.Id;
                    purchaseRequest.RequestStatus = request.NewStatus;
                    purchaseRequest.RevisionDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
                    break;   
                }
                default:
                    return _errorManager.ThrowBadRequest<bool>("El nuevo estado de la solicitud no es válido", "ERP:INVALID_STATUS_CHANGE");
            }

            //✅Finalizar el aprobado de la solicitud de compra y guardar los cambios
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}