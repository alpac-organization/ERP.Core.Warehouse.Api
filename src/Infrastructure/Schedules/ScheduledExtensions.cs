using Quartz;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Warehouse.Api.Infrastructure.Schedules.Jobs;

namespace ERP.Core.Warehouse.Api.Infrastructure.Schedules
{
    public static class ScheduledExtensions
    {
        public static IServiceCollection AddScheduledServices(this IServiceCollection services)
        {
            services.AddQuartz(quartz =>
            {
                var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time"); // Linux: "America/Managua"

                // Programar eventos
                var setKey = new JobKey("WarehouseClockSetEvents");
                quartz.AddJob<WarehouseStartProcessToStartClockJob>(opts => opts.WithIdentity(setKey));
                quartz.AddTrigger(opts => opts
                    .ForJob(setKey)
                    .WithIdentity("WarehouseClockSetEvents-trigger")
                    .WithCronSchedule("0 9 18 * * ?", x => x.InTimeZone(managuaTimeZone)));

                // Limpiar eventos
                var clearKey = new JobKey("WarehouseClockClearEvents");
                quartz.AddJob<WarehouseStartProcessToStopClockJob>(opts => opts.WithIdentity(clearKey));
                quartz.AddTrigger(opts => opts
                    .ForJob(clearKey)
                    .WithIdentity("WarehouseClockClearEvents-trigger")
                    .WithCronSchedule("0 20 17 * * ?", x => x.InTimeZone(managuaTimeZone)));
            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            return services;
        }
    }
}