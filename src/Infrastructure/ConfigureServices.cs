using ERP.Core.Database.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Warehouse.Api.Infrastructure.Services;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
namespace ERP.Core.Manager.Api.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // services.AddJobScheduling();
            services.AddErpDatabaseServices(configuration);

            services.AddHttpClient<IScaleServices, ScaleServices>();

            return services;
        }
    }
}