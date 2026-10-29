using AutoMapper;
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

            var visibleRequestIds = _visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities,
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .Select(pr => pr.Id);

            var purchaseOrdersQuery = _unitOfWork.PurchaseOrders.Entities
                .Where(purs => purs.IsActive)
                .Include(purs => purs.SentByUser)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)

                .Where(purs => purs.PurchaseRequest.Branch.CompanyId == request.CompanyId)
                .Where(purs => visibleRequestIds.Contains(purs.PurchaseRequestId))
                .AsNoTracking();

            if (request.AreaId.HasValue)
            {
                purchaseOrdersQuery = purchaseOrdersQuery
                    .Where(purs => purs.PurchaseRequest.AreaId == request.AreaId);
            }

            if (request.BranchId.HasValue)
            {
                purchaseOrdersQuery = purchaseOrdersQuery
                    .Where(purs => purs.PurchaseRequest.BranchId == request.BranchId);
            }

            var totalRecords = await purchaseOrdersQuery.CountAsync(cancellationToken);

            var purchaseOrders = await purchaseOrdersQuery
                .OrderByDescending(purs => purs.SentToReviewAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var purchaseOrdersMapped = _mapper.Map<List<PurchaseOrderDto>>(purchaseOrders);

            return new PagedResponse<PurchaseOrderDto>(
                purchaseOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
