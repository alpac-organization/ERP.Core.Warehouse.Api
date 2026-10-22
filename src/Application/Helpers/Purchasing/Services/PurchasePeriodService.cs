using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public class PurchasePeriodService : IPurchasePeriodService
    {
        public DateOnly ResolveRequestPeriod(PurchaseRequestType requestType, DateOnly today)
        {
            if (requestType != PurchaseRequestType.Monthly)
            {
                return today;
            }

            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var cutoffDay = daysInMonth - 1;

            if (today.Day >= cutoffDay)
            {
                var firstOfNextMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
                return firstOfNextMonth;
            }

            return today;
        }
    }
}
