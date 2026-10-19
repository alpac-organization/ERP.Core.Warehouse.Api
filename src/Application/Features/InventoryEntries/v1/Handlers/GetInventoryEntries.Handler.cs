using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Handlers
{
    public class GetInventoryEntriesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper _mapper) : BaseValidatorHandler<GetInventoryEntriesQuery, PagedResponse<InventoryEntryDto>>(unitOfWork, errorManager)
    {
        public override async Task<PagedResponse<InventoryEntryDto>> Handle(GetInventoryEntriesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var inventoryEntriesQuery = _unitOfWork.InventoryEntries.Entities
                .Include(inventory => inventory.PurchaseRequest)
                .AsNoTracking()
                .AsSplitQuery();
        
            if (request.BranchId.HasValue && access.Role?.RoleType == RoleType.Administrator)
            {
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory => inventory.BranchId == request.BranchId);
            }
            else
            {
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory => inventory.BranchId == access.Profile.BranchId);
            }

            if (!string.IsNullOrEmpty(request.PurchaseRequestCode))
            {
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory => inventory.PurchaseRequest.Code == request.PurchaseRequestCode);   
            }

            var totalRecords = await inventoryEntriesQuery.CountAsync(cancellationToken);

            var inventoryEntries = await inventoryEntriesQuery
                .OrderBy(m => m.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var inventoryEntriesMapped = _mapper.Map<List<InventoryEntryDto>>(inventoryEntries);

            return new PagedResponse<InventoryEntryDto>(
                inventoryEntriesMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}