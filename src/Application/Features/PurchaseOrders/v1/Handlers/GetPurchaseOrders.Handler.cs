using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrdersHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetPurchaseOrdersQuery, PagedResponse<PurchaseOrderDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var purchaseOrdersQuery = _unitOfWork.PurchaseOrders.Entities
                .Where(purs => purs.IsActive)
                .Include(purs => purs.SentByUser)
                    .ThenInclude(user => user.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                        .ThenInclude(us => us.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)

                .Where(purs => purs.PurchaseRequest.Branch.CompanyId == request.CompanyId)
                .AsNoTracking();

            if (access.Role?.RoleType != RoleType.Administrator && access.Role?.RoleType != RoleType.Supervisor)
            {
                if (access.Role?.RoleType == RoleType.Operator)
                {
                    purchaseOrdersQuery = purchaseOrdersQuery
                        .Where(purs => purs.SentByUserId == request.UserId);
                }

                if (access.Role?.RoleType == RoleType.Manager)
                {
                    purchaseOrdersQuery = purchaseOrdersQuery
                        .Where(purs => purs.PurchaseRequest.AreaId == access.User.AreaId);
                }
            }

            if (request.AreaId.HasValue)
            {
                purchaseOrdersQuery = purchaseOrdersQuery
                    .Where(purs => purs.PurchaseRequest.AreaId == request.AreaId);
            }

            if (request.BranchId.HasValue)
            {
                purchaseOrdersQuery = purchaseOrdersQuery
                    .Where(purs => purs.PurchaseRequest.BranchId == request.BranchId);
            }

            var totalRecords = await purchaseOrdersQuery.CountAsync(cancellationToken);

            var purchaseOrders = await purchaseOrdersQuery
                .OrderByDescending(purs => purs.SentToReviewAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var purchaseOrdersMapped = _mapper.Map<List<PurchaseOrderDto>>(purchaseOrders);

            return new PagedResponse<PurchaseOrderDto>(
                purchaseOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
