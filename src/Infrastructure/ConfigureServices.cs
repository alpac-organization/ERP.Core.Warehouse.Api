using Microsoft.Extensions.Configuration;
using ERP.Core.Application.Commons.Interfaces;
using Microsoft.Extensions.DependencyInjection;

using ERP.Core.Infrastructure;
using ERP.Core.Infrastructure.Services;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Infrastructure;

using ERP.Core.Warehouse.Api.Infrastructure.Services;
using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories.Shopping;
using ERP.Core.Database.Infrastructure.Persistence.Repositories.Shopping;

namespace ERP.Core.Warehouse.Api.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            //Uso de copies notifications
            services.Configure<PurchaseRequestOptions>(
                configuration.GetSection("Notifications:PurchaseRequest")
            );

            services.Configure<Dictionary<PurchaseRequestStatus, ProcessPurchaseRequestOptions>>(
                configuration.GetSection("Notifications:ProcessPurchaseRequest")
            );
            
            // services.AddJobScheduling();
            services.AddErpCoreServices(configuration);
            services.AddErpDatabaseServices(configuration);

            services.AddScoped<IServicesOrdersRequisitionsRepository, ServicesOrdersRequisitionsRepository>();


            services.AddScoped<IErrorManager, ErrorManager>();
            services.AddHttpClient<IScaleServices, ScaleServices>();    
            return services;
        }
    }
}

