using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

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

            var purchaseRequestsQuery = _unitOfWork.PurchaseRequests.Entities
                .Where(purs => purs.IsActive)
                .Include(purs => purs.Branch)
                .AsNoTracking();

            if (access.Role?.RoleType != RoleType.Administrator && access.Role?.RoleType != RoleType.Supervisor)
            {

                if (access.Role?.RoleType == RoleType.Operator)
                {  
                    //Obtener unicamente las solicitudes del usuario que genero sus solocitudes
                    purchaseRequestsQuery = purchaseRequestsQuery
                        .Where(pur => pur.RegisteredByUserId == request.UserId);
                }

                if (access.Role?.RoleType == RoleType.Manager)
                {
                    //Obtener todas las solicitudes del area del usuario
                    purchaseRequestsQuery = purchaseRequestsQuery
                        .Where(pur => pur.AreaId == access.User.AreaId);
                }
            }

            if (request.AreaId.HasValue && access.Role?.RoleType == RoleType.Administrator)
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(pur => pur.AreaId == request.AreaId);
            }

            if (!string.IsNullOrEmpty(request.Code))
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.Code == request.Code);
            }

            if (request.Status.HasValue)
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.RequestStatus == request.Status);
            }

            if (request.RequestType.HasValue)
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.RequestType == request.RequestType);
            }

            if (request.BranchId.HasValue)
            {
                purchaseRequestsQuery = purchaseRequestsQuery
                    .Where(purs => purs.BranchId == request.BranchId);
            }

            var totalRecords = await purchaseRequestsQuery.CountAsync(cancellationToken);

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
                totalRecords
            );
        }
    }
    
}