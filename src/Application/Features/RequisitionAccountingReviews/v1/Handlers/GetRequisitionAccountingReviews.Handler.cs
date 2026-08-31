using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Handlers
{
    public class GetRequisitionAccountingReviewsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetRequisitionAccountingReviewsQuery, PagedResponse<RequisitionAccountingReviewDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<RequisitionAccountingReviewDto>> Handle(GetRequisitionAccountingReviewsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var reviewsQuery = _unitOfWork.RequisitionAccountingReviews.Entities
                .Include(rev => rev.SentByUser)
                    .ThenInclude(user => user.WorkArea)
                .Include(rev => rev.PurchaseRequest)
                .AsNoTracking();

            if (request.BranchId.HasValue)
            {
                reviewsQuery = reviewsQuery
                    .Where(rev => rev.PurchaseRequest.BranchId == request.BranchId);
            }
            else
            {
                reviewsQuery = reviewsQuery
                    .Where(rev => rev.PurchaseRequest.Branch.Company.Id == request.CompanyId);
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

            var reviewsMapped = _mapper.Map<List<RequisitionAccountingReviewDto>>(reviews);

            return new PagedResponse<RequisitionAccountingReviewDto>(
                reviewsMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
