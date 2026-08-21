using ERP.Core.Database.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Warehouse.Api.Infrastructure.Services;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Infrastructure.Services;
using ERP.Core.Infrastructure;

namespace ERP.Core.Warehouse.Api.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // services.AddJobScheduling();
            services.AddErpDatabaseServices(configuration);

            services.AddErpCoreServices(configuration);
            services.AddScoped<IErrorManager, ErrorManager>();
            services.AddScoped<IWarehouseCapacityCalculator, WarehouseCapacityCalculator>();
            services.AddHttpClient<IScaleServices, ScaleServices>();    
            return services;
        }
    }
}
