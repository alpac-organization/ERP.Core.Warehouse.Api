using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class GetKardexMovementHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper _mapper)
        : BaseValidatorHandler<GetKardexMovementQuery, PagedResponse<KardexDto>>(unitOfWork, errorManager)
    {
        public override async Task<PagedResponse<KardexDto>> Handle(GetKardexMovementQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var roleType = access.Role?.RoleType;
            var isAdministrator = roleType == RoleType.Administrator;
            var isManager = roleType == RoleType.Manager;

            var kardexQuery = _unitOfWork.Kardex.Entities
                .AsNoTracking()
                .Where(kproduct => kproduct.DeletedAt == null);

            if (isAdministrator)
            {
                kardexQuery = kardexQuery
                    .Where(kproduct => kproduct.Product.Branch.CompanyId == request.CompanyId);

                if (request.BranchId.HasValue)
                {
                    kardexQuery = kardexQuery
                        .Where(kproduct => kproduct.Product.BranchId == request.BranchId.Value);
                }

                if (request.CostCenterId.HasValue)
                {
                    kardexQuery = kardexQuery
                        .Where(kproduct => kproduct.CostCenterId == request.CostCenterId.Value);
                }

                if (request.WorkAreaId.HasValue)
                {
                    kardexQuery = kardexQuery
                        .Where(kproduct => kproduct.WorkAreaId == request.WorkAreaId.Value);
                }
            }
            else if (isManager)
            {
                kardexQuery = kardexQuery
                    .Where(kproduct => kproduct.Product.BranchId == access.Profile.BranchId);

                if (request.CostCenterId.HasValue)
                {
                    kardexQuery = kardexQuery
                        .Where(kproduct => kproduct.CostCenterId == request.CostCenterId.Value);
                }

                if (request.WorkAreaId.HasValue)
                {
                    kardexQuery = kardexQuery
                        .Where(kproduct => kproduct.WorkAreaId == request.WorkAreaId.Value);
                }
            }
            else
            {
                kardexQuery = kardexQuery
                    .Where(kproduct => kproduct.Product.BranchId == access.Profile.BranchId)
                    .Where(kproduct => kproduct.WorkAreaId == access.Profile.AreaId)
                    .Where(kproduct => kproduct.CostCenterId == access.Profile.CostCenterId);
            }

            if (request.CategoryId.HasValue)
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.Product.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.ProductCode))
            {
                var trimmedCode = request.ProductCode.Trim();
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.Product.Code == trimmedCode);
            }

            if (request.KardexType.HasValue)
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.KardexType == request.KardexType.Value);
            }

            if (request.ReferenceType.HasValue)
            {
                kardexQuery = request.ReferenceType.Value switch
                {
                    ReferenceType.FT => kardexQuery.Where(k =>
                        k.ReferenceType == ReferenceType.FT ||
                        k.InventoryEntryId != null ||
                        k.MovementType == KardexMovementType.Buys),
                    ReferenceType.BL => kardexQuery.Where(k =>
                        k.ReferenceType == ReferenceType.BL ||
                        (k.InventoryOutput != null && k.InventoryOutput.FuelRequestId != null)),
                    ReferenceType.RQ => kardexQuery.Where(k =>
                        k.ReferenceType == ReferenceType.RQ ||
                        (k.InventoryOutput != null && k.InventoryOutput.SupplyRequestId != null)),
                    _ => kardexQuery.Where(k =>
                        k.ReferenceType == ReferenceType.None &&
                        k.InventoryEntryId == null &&
                        k.MovementType != KardexMovementType.Buys &&
                        (k.InventoryOutput == null ||
                         (k.InventoryOutput.FuelRequestId == null && k.InventoryOutput.SupplyRequestId == null)))
                };
            }

            if (request.KardexMovementType.HasValue)
            {
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.MovementType == request.KardexMovementType.Value);
            }

            if (request.DateFrom.HasValue)
            {
                var dateFrom = DateOnly.FromDateTime(request.DateFrom.Value);
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.TransactionDate >= dateFrom);
            }

            if (request.DateTo.HasValue)
            {
                var dateTo = DateOnly.FromDateTime(request.DateTo.Value);
                kardexQuery = kardexQuery
                    .Where(kardex => kardex.TransactionDate <= dateTo);
            }

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var totalRecords = await kardexQuery.CountAsync(cancellationToken);

            var kardex = await kardexQuery
                .Include(kproduct => kproduct.WorkArea)
                .Include(kproduct => kproduct.CostCenter)
                .Include(kproduct => kproduct.InventoryEntry)
                    .ThenInclude(entry => entry!.PurchaseOrder)
                .Include(kproduct => kproduct.InventoryOutput)
                    .ThenInclude(output => output!.SupplyRequest)
                .Include(kproduct => kproduct.InventoryOutput)
                    .ThenInclude(output => output!.FuelRequest)
                .Include(kproduct => kproduct.Product)
                    .ThenInclude(product => product.Category)
                .Include(kproduct => kproduct.Product)
                    .ThenInclude(product => product.UnitMeasure)
                .Include(kproduct => kproduct.Product)
                    .ThenInclude(product => product.Branch)
                .AsSplitQuery()
                .OrderByDescending(kardex => kardex.CreatedAt)
                .ThenByDescending(kardex => kardex.TransactionDate)
                .ThenByDescending(kardex => kardex.TransactionTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var kardexMapped = _mapper.Map<List<KardexDto>>(kardex);

            return new PagedResponse<KardexDto>(
                kardexMapped,
                pageNumber,
                pageSize,
                totalRecords
            );
        }
    }
}
