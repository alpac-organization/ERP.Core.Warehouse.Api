using Microsoft.Extensions.DependencyInjection;

namespace ERP.Core.Warehouse.Api.Infrastructure.Schedules
{
    public static class ScheduledExtensions
    {
        public static void AddScheduledServices(this IServiceCollection services)
        {
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");


        }
    }
}