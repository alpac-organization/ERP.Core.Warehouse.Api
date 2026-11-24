using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class PatchInventoryStockHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<PatchInventoryStockCommand, Unit>(unitOfWork, errorManager)
    {
        private const decimal MaxAllowedStockValue = 99999.99m;

        public override async Task<Unit> Handle(PatchInventoryStockCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var roleType = access.Role?.RoleType;

            if (roleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>(
                    "No tiene acceso para realizar esta acción.",
                    "ERP:INVALID_ACCESS");
            }

            var isAdministrator = roleType == RoleType.Administrator;
            var isManager = roleType == RoleType.Manager;

            var productStockQuery = _unitOfWork.ProductStocks.Entities
                .Include(stock => stock.Product)
                .Where(stock => stock.Id == request.InventoryStockId)
                .Where(stock => stock.IsActive && stock.DeletedAt == null);

            if (isAdministrator)
            {
                productStockQuery = productStockQuery
                    .Where(stock => stock.Product.Branch.CompanyId == request.CompanyId);
            }
            else if (isManager)
            {
                productStockQuery = productStockQuery
                    .Where(stock => stock.Product.BranchId == access.Profile.BranchId);
            }
            else
            {
                productStockQuery = productStockQuery
                    .Where(stock => stock.Product.BranchId == access.Profile.BranchId)
                    .Where(stock => stock.CostCenterId == access.Profile.CostCenterId);
            }

            var productStock = await productStockQuery.FirstOrDefaultAsync(cancellationToken);

            if (productStock is null)
            {
                return _errorManager.ThrowNotFound<Unit>("El control de stock no existe.", "ERP:01");
            }

            if (productStock.Product is null || productStock.Product.DeletedAt != null)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "El producto asociado al control de stock no existe.",
                    "ERP:PRODUCT_NOT_FOUND");
            }

            if (productStock.Product.ProductUsageType != ProductUsageType.Operational)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Solo se pueden editar los niveles de stock de productos operacionales.",
                    "ERP:INVALID_PRODUCT_USAGE_TYPE");
            }

            if (productStock.Product.ProductStatus != ProductStatus.Active)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Solo se pueden editar los niveles de stock de productos activos.",
                    "ERP:PRODUCT_NOT_ACTIVE");
            }

            var newMinimum = request.MinimumStock ?? productStock.MinimumStock;
            var newMaximum = request.MaximumStock ?? productStock.MaximumStock;

            if (newMinimum < 0 || newMinimum > MaxAllowedStockValue)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    $"El stock mínimo debe estar entre 0 y {MaxAllowedStockValue:N2}.",
                    "ERP:02");
            }

            if (newMaximum.HasValue && (newMaximum.Value <= 0 || newMaximum.Value > MaxAllowedStockValue))
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    $"El stock máximo debe ser mayor a 0 y no exceder {MaxAllowedStockValue:N2}.",
                    "ERP:03");
            }

            if (newMaximum.HasValue && newMaximum.Value <= newMinimum)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "El stock máximo debe ser mayor al stock mínimo.",
                    "ERP:04");
            }

            if (request.MinimumStock.HasValue)
            {
                productStock.MinimumStock = request.MinimumStock.Value;
            }

            if (request.MaximumStock.HasValue)
            {
                productStock.MaximumStock = request.MaximumStock.Value;
            }

            if (productStock.AvailableQuantity <= 0)
            {
                productStock.Status = ProductStockStatus.Empty;
            }
            else if (productStock.AvailableQuantity <= productStock.MinimumStock)
            {
                productStock.Status = ProductStockStatus.LowBalance;
            }
            else
            {
                productStock.Status = ProductStockStatus.Stable;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}