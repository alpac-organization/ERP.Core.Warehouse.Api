using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public interface IPurchasePeriodService
    {
        DateOnly ResolveRequestPeriod(PurchaseRequestType requestType, DateOnly todayUtc);
    }
}
