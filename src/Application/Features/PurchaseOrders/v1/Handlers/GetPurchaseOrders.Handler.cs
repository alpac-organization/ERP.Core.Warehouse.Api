using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrdersHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        IMapper _mapper,
        IPurchaseRequestVisibilityService _visibilityService)
        : BaseValidatorHandler<GetPurchaseOrdersQuery, PagedResponse<PurchaseOrderDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var profileCompanyId = access.Profile!.CompanyId;

            var visibleRequestIds = _visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities,
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .Select(pr => pr.Id);

            var purchaseOrdersQuery = _unitOfWork.PurchaseOrders.Entities
                .AsNoTracking()
                .Where(order => order.IsActive)
                .Where(order => order.PurchaseRequest.Branch.CompanyId == request.CompanyId)
                .Where(order => visibleRequestIds.Contains(order.PurchaseRequestId));

            if (request.AreaId.HasValue)
            {
                purchaseOrdersQuery = purchaseOrdersQuery
                    .Where(order => order.PurchaseRequest.AreaId == request.AreaId);
            }

            if (request.BranchId.HasValue)
            {
                purchaseOrdersQuery = purchaseOrdersQuery
                    .Where(order => order.PurchaseRequest.BranchId == request.BranchId);
            }

            var totalRecords = await purchaseOrdersQuery.CountAsync(cancellationToken);

            var purchaseOrdersMapped = await purchaseOrdersQuery
                .OrderByDescending(order => order.SentToReviewAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<PurchaseOrderDto>(
                    _mapper.ConfigurationProvider,
                    new { profileCompanyId })
                .ToListAsync(cancellationToken);

            return new PagedResponse<PurchaseOrderDto>(
                purchaseOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
