using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Handlers
{
    public class GetRequisitionManagementReviewsHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        IMapper _mapper,
        IPurchaseRequestVisibilityService _visibilityService)
        : BaseValidatorHandler<GetRequisitionManagementReviewsQuery, PagedResponse<PurchaseRequestsReviewedManagementDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<PurchaseRequestsReviewedManagementDto>> Handle(GetRequisitionManagementReviewsQuery request, CancellationToken cancellationToken)
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

            var reviewsQuery = _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .Where(rev => rev.DeletedAt == null)
                .Include(rev => rev.SentByUser)
                    .ThenInclude(pur => pur.Profiles
                        .Where(profile => profile.CompanyId == profileCompanyId)
                        .Take(1)
                    )
                    .ThenInclude(profile => profile.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.PurchaseRequestItems)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.CostCenter)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.UserRevision)
                        .ThenInclude(pur => pur.Profiles
                            .Where(profile => profile.CompanyId == profileCompanyId)
                            .Take(1)
                        )
                        .ThenInclude(profile => profile.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.RegistrationUser)
                        .ThenInclude(pur => pur.Profiles
                            .Where(profile => profile.CompanyId == profileCompanyId)
                            .Take(1)
                        )
                        .ThenInclude(profile => profile.WorkArea)
                .Where(rev => rev.PurchaseRequest.Branch.Company.Id == request.CompanyId)
                .Where(rev => visibleRequestIds.Contains(rev.PurchaseRequestId))
                .AsNoTracking();

            if (request.BranchId.HasValue)
            {
                reviewsQuery = reviewsQuery
                    .Where(rev => rev.PurchaseRequest.BranchId == request.BranchId);
            }


            if (request.AreaId.HasValue)
            {
                reviewsQuery = reviewsQuery
                    .Where(rev => rev.PurchaseRequest.AreaId == request.AreaId);
            }

            if (request.Status.HasValue)
            {
                reviewsQuery = reviewsQuery
                    .Where(rev => rev.Status == request.Status);
            }

            var totalRecords = await reviewsQuery.CountAsync(cancellationToken);

            var reviews = await reviewsQuery
                .OrderByDescending(rev => rev.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var reviewsMapped = _mapper.Map<List<PurchaseRequestsReviewedManagementDto>>(reviews);

            return new PagedResponse<PurchaseRequestsReviewedManagementDto>(
                reviewsMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
