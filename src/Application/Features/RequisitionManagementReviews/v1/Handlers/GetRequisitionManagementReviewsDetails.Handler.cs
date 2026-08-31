using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Handlers
{
    public class GetRequisitionManagementReviewsDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetRequisitionManagementReviewsDetailsQuery, PurchaseRequestsReviewedManagementDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseRequestsReviewedManagementDetailsDto> Handle(GetRequisitionManagementReviewsDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var reviewsQuery = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .Include(rev => rev.SentByUser)
                    .ThenInclude(user => user.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.PurchaseRequestItems)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.RegistrationUser)
                        .ThenInclude(rev => rev.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.Branch)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.UserRevision)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(rev => rev.WorkArea)

                .Where(review => review.Id == request.RequisitionManagementReviewsId)
                .FirstOrDefaultAsync(cancellationToken);

            var mapped =  _mapper.Map<PurchaseRequestsReviewedManagementDetailsDto>(reviewsQuery);

            return mapped;
        }
    }
}
