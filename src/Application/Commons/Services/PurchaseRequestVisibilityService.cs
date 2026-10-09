using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public class PurchaseRequestVisibilityService : IPurchaseRequestVisibilityService
    {
        public IQueryable<PurchaseRequest> Apply(
            IQueryable<PurchaseRequest> query,
            RoleType? roleType,
            Guid userId,
            Guid? areaId,
            Guid? costCenterId,
            Guid? branchId)
        {
            ArgumentNullException.ThrowIfNull(query);

            if (roleType is null or RoleType.Administrator or RoleType.Supervisor)
            {
                return query;
            }

            if (roleType == RoleType.Operator)
            {
                return query.Where(pr => pr.RegisteredByUserId == userId);
            }

            if (roleType == RoleType.Manager)
            {
                return query
                    .Where(pr => pr.AreaId == areaId)
                    .Where(pr => pr.CostCenterId == costCenterId)
                    .Where(pr => pr.BranchId == branchId);
            }

            return query;
        }
    }
}
