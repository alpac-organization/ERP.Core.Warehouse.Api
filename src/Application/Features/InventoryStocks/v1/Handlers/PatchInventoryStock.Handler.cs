using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class PatchInventoryStockHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<PatchInventoryStockCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(PatchInventoryStockCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var productStock = await _unitOfWork.ProductStocks.Entities
                .Where(stock => stock.Id == request.InventoryStockId)
                .FirstOrDefaultAsync(cancellationToken);

            if (productStock is null)
            {
                return _errorManager.ThrowNotFound<Unit>("El control de stock no existe.", "ERP:01");
            }

            var newMinimum = request.MinimumStock ?? productStock.MinimumStock;
            var newMaximum = request.MaximumStock ?? productStock.MaximumStock;

            if (newMinimum < 0)
            {
                return _errorManager.ThrowBadRequest<Unit>("El stock mínimo no puede ser menor a 0.", "ERP:02");
            }

            if (newMaximum < 0)
            {
                return _errorManager.ThrowBadRequest<Unit>("El stock máximo no puede ser menor a 0.", "ERP:03");
            }

            if (newMaximum <= newMinimum)
            {
                return _errorManager.ThrowBadRequest<Unit>("El stock máximo debe ser mayor al stock mínimo.", "ERP:04");
            }

            if (request.MinimumStock.HasValue)
            {
                productStock.MinimumStock = request.MinimumStock.Value;
            }

            if (request.MaximumStock.HasValue)
            {
                productStock.MaximumStock = request.MaximumStock.Value;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}