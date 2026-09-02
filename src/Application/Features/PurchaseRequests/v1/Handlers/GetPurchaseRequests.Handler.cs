using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

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

            //Inicializar IQuerable<T>
            var purchaseRequestsQuery = _unitOfWork.PurchaseRequests.Entities
                .Where(purs => purs.IsActive)
                .Include(purs => purs.Branch)
                .Where(purs => purs.Branch.CompanyId == request.CompanyId)
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

            purchaseRequestsQuery = ApplyRequestFilters(purchaseRequestsQuery, request, access);

            purchaseRequestsQuery = ApplyPeriodFilter(purchaseRequestsQuery, request);

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

        #region Filtros periodos
        private static IQueryable<PurchaseRequest> ApplyPeriodFilter(IQueryable<PurchaseRequest> query,GetPurchaseRequestsQuery request)
        {
            var year  = request.Year   ?? DateTime.UtcNow.Year;
            var month = request.Month ?? DateTime.UtcNow.Month;

            var firstDayOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            return query
                .Where(pr => pr.CreatedAt >= firstDayOfMonth && pr.CreatedAt < firstDayOfNextMonth);
        }
        #endregion 

        #region Filtros de busqueda
        private static IQueryable<PurchaseRequest> ApplyRequestFilters(IQueryable<PurchaseRequest> query, GetPurchaseRequestsQuery request, AccessValidationResult<PagedResponse<PurchaseRequestDto>> access)
        {
            if (request.AreaId.HasValue && access.Role?.RoleType == RoleType.Administrator)
            {
                query = query
                    .Where(pr => pr.AreaId == request.AreaId);
            }
 
            if (request.PriorityLevel.HasValue)
            {
                query = query
                    .Where(pr => pr.PriorityLevel == request.PriorityLevel);
            }
 
            if (request.Destination.HasValue)
            {
                query = query
                    .Where(pr => pr.Destination == request.Destination);
            }
 
            if (!string.IsNullOrEmpty(request.Code))
            {
                query = query
                    .Where(pr => pr.Code == request.Code);
            }
 
            if (request.Status.HasValue)
            {
                query = query
                    .Where(pr => pr.RequestStatus == request.Status);
            }
 
            if (request.RequestType.HasValue)
            {
                query = query
                    .Where(pr => pr.RequestType == request.RequestType);
            }
 
            if (request.BranchId.HasValue)
            {
                query = query
                    .Where(pr => pr.BranchId == request.BranchId);
            }
 
            return query;
        }
        #endregion
    }
    
}