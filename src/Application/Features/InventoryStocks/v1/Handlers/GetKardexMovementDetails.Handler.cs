using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class GetKardexMovementDetailsHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper)
        : BaseValidatorHandler<GetKardexMovementDetailsQuery, KardexDetailsDto>(unitOfWork, errorManager)
    {
        public override async Task<KardexDetailsDto> Handle(GetKardexMovementDetailsQuery request, CancellationToken cancellationToken)
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
                .AsSplitQuery()
                .Include(k => k.WorkArea)
                .Include(k => k.CostCenter)
                .Include(k => k.Product)
                    .ThenInclude(p => p.Category)
                .Include(k => k.Product)
                    .ThenInclude(p => p.UnitMeasure)
                .Include(k => k.Product)
                    .ThenInclude(p => p.Branch)
                .Include(k => k.User)
                    .ThenInclude(u => u.Profiles.Where(profile => profile.DeletedAt == null))
                        .ThenInclude(profile => profile.WorkArea)
                .Include(k => k.InventoryEntry)
                    .ThenInclude(entry => entry!.PurchaseOrder)
                        .ThenInclude(po => po!.Supplier)
                .Include(k => k.InventoryEntry)
                    .ThenInclude(entry => entry!.PurchaseOrder)
                        .ThenInclude(po => po!.PurchaseRequest)
                .Include(k => k.InventoryOutput)
                    .ThenInclude(output => output!.ReasonsInventoryRemoval)
                .Include(k => k.InventoryOutput)
                    .ThenInclude(output => output!.SupplyRequest)
                .Include(k => k.InventoryOutput)
                    .ThenInclude(output => output!.FuelRequest)
                .Where(k => k.Id == request.KardexId && k.DeletedAt == null);

            if (isAdministrator)
            {
                kardexQuery = kardexQuery
                    .Where(k => k.Product.Branch.CompanyId == request.CompanyId);
            }
            else if (isManager)
            {
                kardexQuery = kardexQuery
                    .Where(k => k.Product.BranchId == access.Profile.BranchId);
            }
            else
            {
                kardexQuery = kardexQuery
                    .Where(k => k.Product.BranchId == access.Profile.BranchId)
                    .Where(k => k.WorkAreaId == access.Profile.AreaId)
                    .Where(k => k.CostCenterId == access.Profile.CostCenterId);
            }

            var kardex = await kardexQuery.FirstOrDefaultAsync(cancellationToken);

            if (kardex is null)
            {
                return _errorManager.ThrowNotFound<KardexDetailsDto>(
                    "El movimiento de kardex no fue encontrado.",
                    "ERP:KARDEX_MOVEMENT_NOT_FOUND");
            }

            return mapper.Map<KardexDetailsDto>(kardex);
        }
    }
}
