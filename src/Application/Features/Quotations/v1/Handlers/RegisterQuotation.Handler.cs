using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;
using Microsoft.EntityFrameworkCore;


namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class RegisterQuotationHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<RegisterQuotationHandler> _logger) :  BaseValidatorHandler<RegisterQuotationCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(RegisterQuotationCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            _logger.LogInformation("🚩Iniciando registro de cotizaciones.");

            foreach (var quotation in request.QuotationItems)
            {

                var item = await _unitOfWork.PurchaseRequestItems.Entities
                    .Where(pur => pur.Id == quotation.PurchaseRequestItemId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (item is null)
                {
                    _logger.LogInformation("❌No se encontro la información del item requisado");
                    continue;
                }

                var totalToPay = item.Quantity * quotation.Price;

                var quotationEntity = QuotationsMapper.ToQuotationsEntity(quotation);

                quotationEntity.PriceTotal = totalToPay;

                var counts = await _unitOfWork.Quotations.Entities
                    .Where(quo => quo.IsActive)
                    .Where(quo => quo.PurchaseRequestItemId == quotation.PurchaseRequestItemId)
                    .CountAsync(cancellationToken);

                if (counts > 0)
                {
                    item.HasQuotation = true;
                    await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);
                }

                await _unitOfWork.Quotations.RegisterQuotation(quotationEntity);
            }
 
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cotización agregada con exito✅");
            return true;
        }
    }
}