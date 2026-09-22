using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetPurchaseRequestDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetPurchaseRequestDetailsQuery, PurchaseRequestDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseRequestDetailsDto> Handle(GetPurchaseRequestDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Include(pur => pur.Branch)
                .Include(pur => pur.RegistrationUser)
                    .ThenInclude(pur => pur.Profiles
                        .Where(profile => profile.CompanyId == access.Profile.CompanyId)
                        .Take(1)
                    )
                    .ThenInclude(profile => profile.WorkArea)

                .Include(pur => pur.UserRevision)
                    .ThenInclude(pur => pur.Profiles
                        .Where(profile => profile.CompanyId == access.Profile.CompanyId)
                        .Take(1)
                    )
                    .ThenInclude(profile => profile.WorkArea)

                .Include(pur => pur.WorkArea)

                .Include(pur => pur.PurchaseRequestItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.Category)

                .Include(pur => pur.PurchaseRequestItems)
                    .ThenInclude(item => item.UnitMeasure)

                .Include(pur => pur.CostCenter)
                
                .Where(pur => pur.Id == request.PurchaseRequestId)
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return _errorManager.ThrowNotFound<PurchaseRequestDetailsDto>("No se encontro el detalle de esta solicitud", "ERP:NOT_FOUND");
            }

            return _mapper.Map<PurchaseRequestDetailsDto>(purchaseRequest);        
        }
    }
    
}