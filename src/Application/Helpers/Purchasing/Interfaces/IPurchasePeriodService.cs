using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces
{
    public interface IPurchasePeriodService
    {
        // aqui determina el periodo correspondiente a una purchase request basado en el tipo y la fecha de hoy.
        DateOnly ResolveRequestPeriod(PurchaseRequestType requestType, DateOnly today);
    }
}
