using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class DeletePurchaseRequestHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) :  BaseValidatorHandler<DeletePurchaseRequestCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(DeletePurchaseRequestCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType != RoleType.Administrator)
            {
                return _errorManager.ThrowForbidden<bool>("No tiene permisos para eliminar solicitudes de compra", "ERP:FORBIDDEN");
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Where(pur => pur.Id == request.PurchaseRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return _errorManager.ThrowBadRequest<bool>("No se encontro la solicitud de compra", "ERP:NOT_FOUND");
            }

            purchaseRequest.IsActive = false;
            purchaseRequest.DeletedAt = DateTime.UtcNow;

            await _unitOfWork.PurchaseRequests.UpdateAsync(purchaseRequest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
    
}