using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Handlers
{
    public class GetRequisitionAccountingReviewDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetRequisitionAccountingReviewDetailsQuery, PurchaseRequestsReviewedAccountingDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseRequestsReviewedAccountingDetailsDto> Handle(GetRequisitionAccountingReviewDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var review = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                //Usuario que envia la solicitud a revición
                .Include(rev => rev.SentByUser)
                    .ThenInclude(user => user.WorkArea)

                //Solicitud de compras
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.Branch)

                //Areas la que va realizada la solicitud            
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.WorkArea)
                        .ThenInclude(area => area.CostCenters)

                //Usuario que registro la solicitud de compra
                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.RegistrationUser)
                        .ThenInclude(user => user.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.UserRevision)
                        .ThenInclude(user => user.WorkArea)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)

                .Include(rev => rev.PurchaseRequest)
                    .ThenInclude(pur => pur.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)

                .AsNoTracking()
                .Where(rev => rev.Id == request.RequisitionAccountingReviewId)
                .FirstOrDefaultAsync(cancellationToken);

            if (review is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseRequestsReviewedAccountingDetailsDto>("No se encontro la revision contable", "ERP:NOT_FOUND");
            }

            return _mapper.Map<PurchaseRequestsReviewedAccountingDetailsDto>(review);
        }
    }
    
}
