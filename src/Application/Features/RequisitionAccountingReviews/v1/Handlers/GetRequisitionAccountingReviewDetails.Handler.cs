using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Handlers
{
    public class GetRequisitionAccountingReviewDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetRequisitionAccountingReviewDetailsQuery, RequisitionAccountingReviewDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<RequisitionAccountingReviewDetailsDto> Handle(GetRequisitionAccountingReviewDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var review = await _unitOfWork.RequisitionAccountingReviews.Entities
                .Include(rev => rev.ReviewedByUser)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.Branch)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.WorkArea)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.RegistrationUser)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.UserRevision)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                            .ThenInclude(product => product.Category)
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations)
                            .ThenInclude(quo => quo.Supplier)
                .AsNoTracking()
                .Where(rev => rev.Id == request.RequisitionAccountingReviewId)
                .FirstOrDefaultAsync(cancellationToken);

            if (review is null)
            {
                return _errorManager.ThrowBadRequest<RequisitionAccountingReviewDetailsDto>("No se encontro la revision contable", "ERP:NOT_FOUND");
            }

            return _mapper.Map<RequisitionAccountingReviewDetailsDto>(review);
        }
    }
    
}
