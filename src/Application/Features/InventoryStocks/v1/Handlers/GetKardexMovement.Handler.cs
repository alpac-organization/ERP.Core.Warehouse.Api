using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class GetKardexMovementHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper _mapper) : BaseValidatorHandler<GetKardexMovementQuery, PagedResponse<KardexDto>>(unitOfWork, errorManager)
    {
        public override async Task<PagedResponse<KardexDto>> Handle(GetKardexMovementQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var kardexQuery = _unitOfWork.Kardex.Entities
                .Include(kproduct => kproduct.WorkArea)
                .Include(kproduct => kproduct.Product)
                .Where(kproduct => kproduct.WorkAreaId == access.Profile.AreaId)
                .Where(kproduct => kproduct.CostCenterId == access.Profile.CostCenterId)
                .AsSplitQuery()
                .AsNoTracking();

            if (request.CategoryId.HasValue)
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.Product.CategoryId == request.CategoryId);
            }
            
            if (!string.IsNullOrEmpty(request.ProductCode))
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.Product.Code == request.ProductCode);
            }

            if (request.KardexType.HasValue)
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.KardexType == request.KardexType);
            }

            if (request.KardexMovementType.HasValue)
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.MovementType == request.KardexMovementType);
            }

            var kardex = await kardexQuery
                .OrderBy(product => product.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var totalRecords = await kardexQuery.CountAsync(cancellationToken);

            var kardexMapped = _mapper.Map<List<KardexDto>>(kardex);

            return new PagedResponse<KardexDto>(
                kardexMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}