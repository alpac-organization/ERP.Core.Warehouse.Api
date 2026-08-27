using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Handlers
{
    public class GetRequisitionManagementReviewsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetRequisitionManagementReviewsQuery, PagedResponse<RequisitionManagementReviewDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<RequisitionManagementReviewDto>> Handle(GetRequisitionManagementReviewsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var reviewsQuery = _unitOfWork.RequisitionManagementReviews.Entities
                .Include(rev => rev.SentByUser)
                    .ThenInclude(user => user.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.PurchaseRequestItems)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.UserRevision)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.RegistrationUser)
                .AsNoTracking();

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

            var reviewsMapped = _mapper.Map<List<RequisitionManagementReviewDto>>(reviews);

            return new PagedResponse<RequisitionManagementReviewDto>(
                reviewsMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
