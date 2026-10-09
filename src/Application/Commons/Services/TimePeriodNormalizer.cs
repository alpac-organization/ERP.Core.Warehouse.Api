using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Commons.Services
{
    public static class TimePeriodNormalizer
    {
       public static decimal? ToDays(decimal? value, TimeType? timeType)
        {
            if (!value.HasValue || value.Value <= 0 || !timeType.HasValue)
            {
                return null;
            }

            return timeType.Value switch
            {
                TimeType.Day => value.Value,
                TimeType.Month => value.Value * 30m,
                TimeType.Year => value.Value * 365m,
                _ => null
            };
        }
    }
}
