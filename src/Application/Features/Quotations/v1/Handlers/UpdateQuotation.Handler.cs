using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class UpdateQuotationHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<UpdateQuotationHandler> _logger) :  BaseValidatorHandler<UpdateQuotationCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(UpdateQuotationCommand request, CancellationToken cancellationToken)
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

            _logger.LogInformation("🚩Iniciando actualización de cotización.");

            var quotation = await _unitOfWork.Quotations.Entities
                .Where(quo => quo.IsActive)
                .Where(quo => quo.Id == request.QuotationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotation is null)
            {
                return _errorManager.ThrowNotFound<bool>("La cotización no existe.", "ERP:QUOTATION_NOT_FOUND");
            }

            if (request.SupplierId.HasValue)
                quotation.SupplierId = request.SupplierId.Value;

            if (request.HasDelivery.HasValue)
                quotation.HasDelivery = request.HasDelivery.Value;

            if (request.HasGuarantee.HasValue)
                quotation.HasGuarantee = request.HasGuarantee.Value;

            if (request.Iva.HasValue)
                quotation.Iva = request.Iva.Value;

            if (request.Price.HasValue)
                quotation.Price = request.Price.Value;

            if (request.PriceUnit.HasValue)
                quotation.PriceUnit = request.PriceUnit.Value;

            if (request.BrandProduct is not null)
                quotation.BrandProduct = request.BrandProduct;

            if (request.DeliveryTime.HasValue)
                quotation.DeliveryTime = request.DeliveryTime.Value;

            if (request.DeliveryTimeType.HasValue)
                quotation.DeliveryTimeType = request.DeliveryTimeType.Value;

            if (request.WarrantyPeriod.HasValue)
                quotation.WarrantyPeriod = request.WarrantyPeriod.Value;

            if (request.WarrantyPeriodTimeType.HasValue)
                quotation.WarrantyPeriodTimeType = request.WarrantyPeriodTimeType.Value;

            if (request.Price.HasValue)
            {
                quotation.Price = request.Price.Value;
                quotation.PriceTotal = RecalculatePriceTotal(quotation.PriceUnit, quotation.Price);
            }

            await _unitOfWork.Quotations.UpdateAsync(quotation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cotización actualizada con éxito✅");
            return true;
        }

        private static decimal RecalculatePriceTotal(decimal? priceUnit, decimal price)
        {
            return (priceUnit ?? 0) * price;
        }
    }
}