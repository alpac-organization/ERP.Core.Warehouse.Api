using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class AcceptQuotationForPurchaseHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) :  BaseValidatorHandler<AcceptQuotationForPurchaseCommand, bool>(_unitOfWork, _errorManager)
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
                .Where(quo => quo.IsActive)
                .Where(quo => quo.Id == request.QuotationId)
                .Where(quo => quo.PurchaseRequestItemId == request.PurchaseRequestItemId)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotation is null)
            {
                return _errorManager.ThrowNotFound<bool>("La cotización no existe o no pertenece al producto solicitado", "ERP:QUOTATION_NOT_FOUND");
            }
            
            var itemQuotations = await _unitOfWork.Quotations.Entities
                .Where(quo => quo.PurchaseRequestItemId == request.PurchaseRequestItemId)
                .Where(quo => quo.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var itemQuotation in itemQuotations)
            {
                itemQuotation.IsAcceptedForPurchase = itemQuotation.Id == request.QuotationId;
                await _unitOfWork.Quotations.UpdateAsync(itemQuotation);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
    
}
