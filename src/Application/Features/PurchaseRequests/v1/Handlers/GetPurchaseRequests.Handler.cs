using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetPurchaseRequestsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetPurchaseRequestsQuery, PagedResponse<PurchaseRequestDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<PurchaseRequestDto>> Handle(GetPurchaseRequestsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<PagedResponse<PurchaseRequestDto>>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var purchaseRequestsQuery = _unitOfWork.PurchaseRequests.Entities
                .Include(purs => purs.Branch)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(request.Code))
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.Code == request.Code);
            }
        

            if (!string.IsNullOrEmpty(request.Code))
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.Code == request.Code);
            }


            if (request.RequestType.HasValue)
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.RequestType == request.RequestType);
            }

            var purchaseRequests = await purchaseRequestsQuery
                .OrderByDescending(quo => quo.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var purchaseRequestsMapped = _mapper.Map<List<PurchaseRequestDto>>(purchaseRequests);

            return new PagedResponse<PurchaseRequestDto>(
                purchaseRequestsMapped,
                request.PageNumber,
                request.PageSize,
                0
            );
        }
    }
    
}