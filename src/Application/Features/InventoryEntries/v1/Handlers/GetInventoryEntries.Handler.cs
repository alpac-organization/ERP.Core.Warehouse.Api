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
            
            var roleType = access.Role?.RoleType;
            var isAdministrator = roleType == RoleType.Administrator;
            var isManager = roleType == RoleType.Manager;

            var inventoryEntriesQuery = _unitOfWork.InventoryEntries.Entities
                .Include(inventory => inventory.PurchaseOrder)
                    .ThenInclude(purchaseOrder => purchaseOrder.PurchaseRequest)
                        .ThenInclude(purchaseRequest => purchaseRequest.WorkArea)
                .Include(inventory => inventory.Branch)
                .Include(inventory => inventory.WorkArea)
                .Where(inventory => inventory.DeletedAt == null)
                .Where(inventory => inventory.Branch.CompanyId == request.CompanyId)
                .AsNoTracking()
                .AsSplitQuery();

            if (isAdministrator)
            {
                if (request.BranchId.HasValue)
                {
                    inventoryEntriesQuery = inventoryEntriesQuery
                        .Where(inventory => inventory.BranchId == request.BranchId.Value);
                }

                if (request.WorkAreaId.HasValue)
                {
                    var areaId = request.WorkAreaId.Value;
                    inventoryEntriesQuery = inventoryEntriesQuery
                        .Where(inventory =>
                            inventory.PurchaseOrder != null && inventory.PurchaseOrder.PurchaseRequest != null
                                ? inventory.PurchaseOrder.PurchaseRequest.AreaId == areaId
                                : inventory.WorkAreaId == areaId);
                }
            }
            else if (isManager)
            {
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory => inventory.BranchId == access.Profile.BranchId);

                if (request.WorkAreaId.HasValue)
                {
                    var areaId = request.WorkAreaId.Value;
                    inventoryEntriesQuery = inventoryEntriesQuery
                        .Where(inventory =>
                            inventory.PurchaseOrder != null && inventory.PurchaseOrder.PurchaseRequest != null
                                ? inventory.PurchaseOrder.PurchaseRequest.AreaId == areaId
                                : inventory.WorkAreaId == areaId);
                }
            }
            else
            {
                var operatorAreaId = access.Profile.AreaId;

                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory => inventory.BranchId == access.Profile.BranchId)
                    .Where(inventory =>
                        inventory.PurchaseOrder != null && inventory.PurchaseOrder.PurchaseRequest != null
                            ? inventory.PurchaseOrder.PurchaseRequest.AreaId == operatorAreaId
                            : inventory.WorkAreaId == operatorAreaId);
            }

            if (request.EntryStatus.HasValue)
            {
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory => inventory.EntryStatus == request.EntryStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.PurchaseRequestCode))
            {
                var requestCode = request.PurchaseRequestCode.Trim().ToUpper();
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory =>
                        inventory.PurchaseOrder != null &&
                        inventory.PurchaseOrder.PurchaseRequest != null &&
                        inventory.PurchaseOrder.PurchaseRequest.Code != null &&
                        inventory.PurchaseOrder.PurchaseRequest.Code.ToUpper().Contains(requestCode));
            }

            if (!string.IsNullOrWhiteSpace(request.EntryCode))
            {
                var entryCode = request.EntryCode.Trim().ToUpper();
                inventoryEntriesQuery = inventoryEntriesQuery
                    .Where(inventory =>
                        inventory.Code != null &&
                        inventory.Code.ToUpper().Contains(entryCode));
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
