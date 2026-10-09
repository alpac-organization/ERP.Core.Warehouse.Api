using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Handlers
{
    public class GetRequisitionManagementReviewsDetailsHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper)
        : BaseValidatorHandler<GetRequisitionManagementReviewsDetailsQuery, PurchaseRequestsReviewedManagementDetailsDto>(
            unitOfWork, errorManager)
    {
        public override async Task<PurchaseRequestsReviewedManagementDetailsDto> Handle(
            GetRequisitionManagementReviewsDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var review = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .Include(rev => rev.SentByUser)
                    .ThenInclude(pur => pur.Profiles
                        .Where(profile => profile.CompanyId == access.Profile.CompanyId)
                        .Take(1))
                    .ThenInclude(profile => profile.WorkArea)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                            .ThenInclude(product => product.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                                .ThenInclude(sp => sp.Supplier)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q =>
                            q.IsActive && q.DeletedAt == null && q.IsAcceptedForPurchase))
                            .ThenInclude(q => q.Supplier)
                                .ThenInclude(s => s.SupplierDetails)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q =>
                            q.IsActive && q.DeletedAt == null && q.IsAcceptedForPurchase))
                            .ThenInclude(q => q.SupplierProduct)
                                .ThenInclude(sp => sp!.TierPrices)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                        .ThenInclude(pur => pur.Profiles
                            .Where(profile => profile.CompanyId == access.Profile.CompanyId)
                            .Take(1))
                        .ThenInclude(profile => profile.WorkArea)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pr => pr.CostCenter)
                .AsNoTracking()
                .Where(review => review.Id == request.RequisitionManagementReviewsId)
                .FirstOrDefaultAsync(cancellationToken);

            return mapper.Map<PurchaseRequestsReviewedManagementDetailsDto>(review);
        }
    }
}
