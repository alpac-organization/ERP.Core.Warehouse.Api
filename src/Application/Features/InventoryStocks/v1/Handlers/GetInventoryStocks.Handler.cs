using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;
using ERP.Core.Database.Domain.Enums;

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

                .Where(inventory => inventory.Product.BranchId == access.Profile.BranchId)
                .Where(inventory => inventory.Product.CostCenterId == access.Profile.CostCenterId)
                .Where(inventory => inventory.Product.CostCenter.WorkAreaId == access.Profile.AreaId)
                .Where(inventory => inventory.Product.ProductUsageType == ProductUsageType.Operational)
                .AsNoTracking()
                .AsSplitQuery();


            var productStocks = await productStocksQuery
                .ToListAsync(cancellationToken);

            var productStocksMapped = _mapper.Map<List<InventoryStockDto>>(productStocks);
            
            return new PagedResponseStock<InventoryStockDto>(
                productStocksMapped,
                request.PageNumber,
                request.PageSize,
                0
            );
        }
    }
}