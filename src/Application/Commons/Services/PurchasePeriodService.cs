using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public class PurchasePeriodService : IPurchasePeriodService
    {
        public DateOnly ResolveRequestPeriod(PurchaseRequestType requestType, DateOnly todayUtc)
        {
            if (requestType != PurchaseRequestType.Monthly)
            {
                return todayUtc;
            }

            var daysInMonth = DateTime.DaysInMonth(todayUtc.Year, todayUtc.Month);
            var cutoffDay = daysInMonth - 1; 

            if (todayUtc.Day >= cutoffDay)
            {
                var firstOfNextMonth = new DateOnly(todayUtc.Year, todayUtc.Month, 1).AddMonths(1);
                return firstOfNextMonth;
            }

            return todayUtc;
        }
    }
}
