using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Database.Infrastructure;

namespace ERP.Core.Manager.Api.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // services.AddJobScheduling();
            services.AddErpDatabaseServices(configuration);

            return services;
        }
    }
}