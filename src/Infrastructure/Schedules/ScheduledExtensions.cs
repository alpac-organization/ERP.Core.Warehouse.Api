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
                var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua"); // Linux: "America/Managua"

                // Programar eventos
                // var setKey = new JobKey("WarehouseClockSetEvents");
                // quartz.AddJob<WarehouseStartProcessToStartClockJob>(opts => opts.WithIdentity(setKey));
                // quartz.AddTrigger(opts => opts
                //     .ForJob(setKey)
                //     .WithIdentity("WarehouseClockSetEvents-trigger")
                //     .WithCronSchedule("0 0 4 * * ?", x => x.InTimeZone(managuaTimeZone)));

                // var clearKey = new JobKey("WarehouseClockClearEvents");
                // quartz.AddJob<WarehouseStartProcessToStopClockJob>(opts => opts.WithIdentity(clearKey));
                // quartz.AddTrigger(opts => opts
                //     .ForJob(clearKey)
                //     .WithIdentity("WarehouseClockClearEvents-trigger")
                //     .WithCronSchedule("0 20 5 * * ?", x => x.InTimeZone(managuaTimeZone)));

                var setKey = new JobKey("WarehouseClockSetEvents");
                quartz.AddJob<WarehouseStartProcessToStartClockJob>(opts => opts.WithIdentity(setKey));
                quartz.AddTrigger(opts => opts
                    .ForJob(setKey)
                    .WithIdentity("WarehouseClockSetEvents-trigger")
                    .WithCronSchedule("0 */5 * * * ?", x => x.InTimeZone(managuaTimeZone))); // Cada 5 minutos

                // var clearKey = new JobKey("WarehouseClockClearEvents");
                // quartz.AddJob<WarehouseStartProcessToStopClockJob>(opts => opts.WithIdentity(clearKey));
                // quartz.AddTrigger(opts => opts
                //     .ForJob(clearKey)
                //     .WithIdentity("WarehouseClockClearEvents-trigger")
                //     .WithCronSchedule("0 * * * * ?", x => x.InTimeZone(managuaTimeZone))); // Cada minuto
            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            return services;
        }
    }
}