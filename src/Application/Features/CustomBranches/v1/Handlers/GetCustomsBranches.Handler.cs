using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Handlers
{
    public class GetCustomsBranchesHandler(IUnitOfWork _unitOfWork, IErrorManager errorManager, IMapper _mapper) : BaseValidatorHandler<GetCustomBranchesQuery, PagedResponse<CustomsBranchDto>>(_unitOfWork, errorManager)
    {
        public override async Task<PagedResponse<CustomsBranchDto>> Handle(GetCustomBranchesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var customsBranchesQuery = _unitOfWork.CustomsBranches.Entities
                .Where(branch => branch.IsActive)
                .AsNoTracking();

            var totalRecords = await customsBranchesQuery.CountAsync(cancellationToken);

            var purchaseOrders = await customsBranchesQuery
                .OrderByDescending(purchase => purchase.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var purchaseOrdersMapped = _mapper.Map<List<CustomsBranchDto>>(purchaseOrders);

            return new PagedResponse<CustomsBranchDto>(
                purchaseOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}