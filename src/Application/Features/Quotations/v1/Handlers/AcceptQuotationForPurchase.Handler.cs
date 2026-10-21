using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class AcceptQuotationForPurchaseHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        IPurchaseRequestVisibilityService _visibilityService)
        : BaseValidatorHandler<AcceptQuotationForPurchaseCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(AcceptQuotationForPurchaseCommand request, CancellationToken cancellationToken)
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

            var quotation = await _unitOfWork.Quotations.Entities
                .Include(quo => quo.PurchaseRequestItem)
                .Where(quo => quo.IsActive)
                .Where(quo => quo.Id == request.QuotationId)
                .Where(quo => quo.PurchaseRequestItemId == request.PurchaseRequestItemId)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotation is null)
            {
                return _errorManager.ThrowNotFound<bool>("La cotización no existe o no pertenece al producto solicitado", "ERP:QUOTATION_NOT_FOUND");
            }

            var isVisible = await _visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities
                        .Where(pr => pr.Id == quotation.PurchaseRequestItem.PurchaseRequestId),
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .AnyAsync(cancellationToken);

            if (!isVisible)
            {
                return _errorManager.ThrowNotFound<bool>(
                    "La solicitud de compra no fue encontrada",
                    "ERP:PURCHASE_REQUEST_NOT_FOUND");
            }

            var itemQuotations = await _unitOfWork.Quotations.Entities
                .Where(quo => quo.PurchaseRequestItemId == request.PurchaseRequestItemId)
                .Where(quo => quo.IsActive)
                .ToListAsync(cancellationToken);

            //Cada producto solicitado debe tener al menos dos cotizaciones activas para poder aceptar una para compra
            if (itemQuotations.Count < 2)
            {
                return _errorManager.ThrowBadRequest<bool>("El producto solicitado debe tener al menos dos cotizaciones para aceptar una para compra", "ERP:NOT_ENOUGH_QUOTATIONS");
            }

            //La cotización ya se encuentra aceptada para compra
            if (quotation.IsAcceptedForPurchase)
            {
                return true;
            }

            //Solo una cotización puede estar aceptada para compra por producto solicitado
            foreach (var itemQuotation in itemQuotations)
            {
                var isAccepted = itemQuotation.Id == request.QuotationId;
                itemQuotation.IsAcceptedForPurchase = isAccepted;
                if (isAccepted)
                {
                    itemQuotation.SupplierSelectionJustification = request.SupplierSelectionJustification;
                    // itemQuotation.SupplierRejectionJustification = null;
                }
                else
                {
                    // itemQuotation.SupplierRejectionJustification = request.SupplierRejectionJustification;
                }
                await _unitOfWork.Quotations.UpdateAsync(itemQuotation);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
