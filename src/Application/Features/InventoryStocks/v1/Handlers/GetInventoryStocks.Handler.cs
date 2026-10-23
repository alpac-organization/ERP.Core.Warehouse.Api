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

            var productStocksQuery = _unitOfWork.ProductStocks.Entities
                .Include(inventory => inventory.Product)
                    .ThenInclude(product => product.Category)
                
                .Include(inventory => inventory.Product)
                    .ThenInclude(product => product.CostCenter)

                .Include(inventory => inventory.CostCenter)

                .Where(inventory => inventory.Product.BranchId == access.Profile.BranchId)
                .Where(inventory => inventory.CostCenterId == access.Profile.CostCenterId)
                .Where(inventory => inventory.Product.CostCenter.WorkAreaId == access.Profile.AreaId)
                .Where(inventory => inventory.Product.ProductUsageType == ProductUsageType.Operational)
                .AsNoTracking()
                .AsSplitQuery();


            if (request.CategoryId.HasValue)
            {
                productStocksQuery = productStocksQuery
                    .Where(inventory => inventory.Product.CategoryId == request.CategoryId);
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

            var productStocks = await productStocksQuery
                .OrderBy(product => product.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var counts = await productStocksQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    TotalRecords = group.Count(),
                    InMinimum = group.Count(inventory => inventory.AvailableQuantity <= inventory.MinimumStock)
                })
                .FirstOrDefaultAsync(cancellationToken);

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