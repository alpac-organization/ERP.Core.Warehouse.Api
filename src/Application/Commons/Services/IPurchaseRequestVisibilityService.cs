using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public interface IPurchaseRequestVisibilityService
    {
        IQueryable<PurchaseRequest> Apply(
            IQueryable<PurchaseRequest> query,
            RoleType? roleType,
            Guid userId,
            Guid? areaId,
            Guid? costCenterId,
            Guid? branchId);
    }
}
