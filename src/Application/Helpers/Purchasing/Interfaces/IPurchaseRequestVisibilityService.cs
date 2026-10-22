using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces
{
    public interface IPurchaseRequestVisibilityService
    {
        // Aplica las restricciones de visibilidad sobre las solicitudes de compra según el tipo de rol, área, centro de costo y sucursal.
        IQueryable<PurchaseRequest> Apply(IQueryable<PurchaseRequest> query, RoleType? roleType, Guid userId, Guid? areaId, Guid? costCenterId, Guid? branchId);
    }
}
