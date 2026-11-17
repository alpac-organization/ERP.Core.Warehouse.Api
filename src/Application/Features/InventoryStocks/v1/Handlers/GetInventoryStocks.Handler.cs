using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;


namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class GetInventoryStocksHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper _mapper) : BaseValidatorHandler<GetInventoryStocksQuery, PagedResponseStock<InventoryStockDto>>(unitOfWork, errorManager)
    {
        public override async Task<PagedResponseStock<InventoryStockDto>> Handle(GetInventoryStocksQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var roleType        = access.Role?.RoleType;
            var isAdministrator = roleType == RoleType.Administrator;
            var isManager       = roleType == RoleType.Manager;

            var productStocksQuery = _unitOfWork.ProductStocks.Entities
                .AsNoTracking()
                .Where(inventory => inventory.Product.ProductUsageType == ProductUsageType.Operational)
                .Where(inventory =>
                    inventory.Product.ProductStatus == ProductStatus.Active ||
                    inventory.Product.ProductStatus == ProductStatus.LowProcess
                );

            // ---------- Filtros por rol (restricciones de seguridad) ----------
            if (isAdministrator)
            {
                // Administrador: sucursal y centro de costos opcionales
                if (request.BranchId.HasValue)
                {
                    productStocksQuery = productStocksQuery
                        .Where(inventory => inventory.Product.BranchId == request.BranchId.Value);
                }

                if (request.CostCenterId.HasValue)
                {
                    productStocksQuery = productStocksQuery
                        .Where(inventory => inventory.CostCenterId == request.CostCenterId.Value);
                }
            }
            else if (isManager)
            {
                // Manager: sucursal fija del perfil, centro de costos opcional
                productStocksQuery = productStocksQuery
                    .Where(inventory => inventory.Product.BranchId == access.Profile.BranchId);

                if (request.CostCenterId.HasValue)
                {
                    productStocksQuery = productStocksQuery
                        .Where(inventory => inventory.CostCenterId == request.CostCenterId.Value);
                }
            }
            else
            {
                // Operador / Supervisor: sucursal, centro de costos y área del perfil
                productStocksQuery = productStocksQuery
                    .Where(inventory => inventory.Product.BranchId == access.Profile.BranchId)
                    .Where(inventory => inventory.CostCenterId == access.Profile.CostCenterId)
                    .Where(inventory => inventory.Product.CostCenter.WorkAreaId == access.Profile.AreaId);
            }

            // ---------- Conteos (respetan la restricción del rol) ----------
            var counts = await productStocksQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    TotalRecords = group.Count(),
                    InMinimum = group.Count(inventory => inventory.AvailableQuantity <= inventory.MinimumStock)
                })
                .FirstOrDefaultAsync(cancellationToken);

            // ---------- Filtros libres ----------
            if (request.CategoryId.HasValue)
            {
                productStocksQuery = productStocksQuery
                    .Where(inventory => inventory.Product.CategoryId == request.CategoryId);
            }

            if (request.IsFuel.HasValue)
            {
                productStocksQuery = productStocksQuery
                    .Where(inventory => inventory.Product.Category != null
                                        && inventory.Product.Category.IsFuel == request.IsFuel.Value);
            }

            if (!string.IsNullOrEmpty(request.ProductCode))
            {
                productStocksQuery = productStocksQuery
                    .Where(inventory => inventory.Product.Code == request.ProductCode);
            }

            if (!string.IsNullOrEmpty(request.ProductName))
            {
                productStocksQuery = productStocksQuery
                    .Where(inventory => EF.Functions.Like(inventory.Product.ProductName, $"%{request.ProductName}%"));
            }

            // ---------- Página ----------
            var productStocks = await productStocksQuery
                .Include(inventory => inventory.Product)
                    .ThenInclude(product => product.Category)
                .Include(inventory => inventory.Product)
                    .ThenInclude(product => product.CostCenter)
                .Include(inventory => inventory.CostCenter)
                .Include(inventory => inventory.UnitMeasure)
                .AsSplitQuery()
                .OrderBy(inventory => inventory.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var productStocksMapped = _mapper.Map<List<InventoryStockDto>>(productStocks);

            return new PagedResponseStock<InventoryStockDto>(
                productStocksMapped,
                request.PageNumber,
                request.PageSize,
                counts?.InMinimum ?? 0,
                counts?.TotalRecords ?? 0
            );
        }
    }
}