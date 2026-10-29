using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetPurchaseRequestProductsHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        IMapper _mapper,
        IPurchaseRequestVisibilityService _visibilityService)
        : BaseValidatorHandler<GetPurchaseRequestsProductsQuery, PagedResponse<PurchaseRequestItemDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<PurchaseRequestItemDto>> Handle(GetPurchaseRequestsProductsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var isVisible = await _visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities.Where(pr => pr.Id == request.PurchaseRequestId),
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .AnyAsync(cancellationToken);

            if (!isVisible)
            {
                return _errorManager.ThrowNotFound<PagedResponse<PurchaseRequestItemDto>>(
                    "No se encontro la solicitud de compra",
                    "ERP:NOT_FOUND");
            }

            var purchaseRequestItemsQuery = _unitOfWork.PurchaseRequestItems.Entities
                .Where(item => item.PurchaseRequestId == request.PurchaseRequestId)
                .Where(item => item.DeletedAt == null)
                .Include(item => item.Product)
                    .ThenInclude(product => product.Category)
                .Include(item => item.Product)
                    .ThenInclude(product => product.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                        .ThenInclude(sp => sp.Supplier)
                .Include(item => item.UnitMeasure)
                .Include(item => item.Quotations.Where(quo => quo.IsActive && quo.DeletedAt == null))
                    .ThenInclude(quote => quote.Supplier)
                        .ThenInclude(supplier => supplier.SupplierDetails)
                .Include(item => item.Quotations.Where(quo => quo.IsActive && quo.DeletedAt == null))
                    .ThenInclude(quote => quote.SupplierProduct)
                .AsNoTracking()
                .AsSplitQuery();

            var totalRecords = await purchaseRequestItemsQuery.CountAsync(cancellationToken);

            var purchaseRequestItems = await purchaseRequestItemsQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var purchaseRequestItemsMapped = _mapper.Map<List<PurchaseRequestItemDto>>(purchaseRequestItems);

            return new PagedResponse<PurchaseRequestItemDto>(
                purchaseRequestItemsMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
