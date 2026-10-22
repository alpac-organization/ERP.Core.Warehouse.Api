using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public class PurchaseRequestVisibilityService : IPurchaseRequestVisibilityService
    {
        public IQueryable<PurchaseRequest> Apply(IQueryable<PurchaseRequest> query, RoleType? roleType, Guid userId, Guid? areaId, Guid? costCenterId, Guid? branchId)
        {
            if (roleType == RoleType.Operator)
            {
                return query.Where(pr => pr.RegisteredByUserId == userId);
            }

            if (roleType == RoleType.Manager)
            {
                if (!areaId.HasValue)
                {
                    return query.Where(_ => false);
                }

                return query.Where(pr => pr.AreaId == areaId);
            }

            if (roleType is RoleType.Supervisor or RoleType.Administrator)
            {
                if (!branchId.HasValue)
                {
                    return query.Where(_ => false);
                }

                return query.Where(pr => pr.BranchId == branchId);
            }

            return query.Where(_ => false);
        }
    }
}
